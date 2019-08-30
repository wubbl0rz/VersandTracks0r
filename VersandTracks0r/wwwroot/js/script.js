var app = new Vue({
    data: {
      showAdd: false,
      addForm: {
        trackingId: "",
        carrier: "",
        comment: ""
      },
      map: null,
      mapItems: [],
      mapTitle: "",
      showMap: false,
      expanded: null,
      shipments: [],
      dark_mode: false,
    },
    methods: {
      checkForm(e) {
        this.addForm.trackingId = this.addForm.trackingId.trim();

        if (this.addForm.trackingId == "" || this.addForm.carrier == "") {
          e.preventDefault();
        }

        if (this.addForm.comment == "") {
          this.addForm.comment = this.addForm.carrier;
        }
      },
      addShipment() {
        if (this.showAdd) {
          this.showAdd = false
          this.addForm.trackingId = "";
          this.addForm.carrier = "";
          this.addForm.comment = "";

        }
        else {
          this.showAdd = true;
        }
      },
      getCarrierLogo(carrier) {
        return carrier.toLocaleLowerCase();
      },
      popupMap(shipment) {
        if (this.showMap) {
          this.showMap = false;
        }
        else {
          this.showMap = true;
          this.mapTitle = "Loading... "

          this.mapItems.forEach(item => {
            this.map.removeLayer(item);
          });

          Vue.nextTick(async () => {
            if (!this.map) {
              this.map = L.map("internalMap").setView([0, 0], 12);
              L.tileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png').addTo(this.map);
            }

            this.map.setView([0, 0], 12);

            const history = shipment.history.filter(e => e.lat != 0 && e.long != 0);

            console.log("==============");
            console.log(history);

            const points = history.map(e => L.latLng(e.lat, e.long));

            this.map.fitBounds(L.latLngBounds(points));

            // checken was ist wenn 0 drin ist
            //const path = L.polyline(points, { className: 'tracking_line', snakingSpeed: 500 });

            const path = L.motion.polyline(points, {
              className: 'tracking_line',
            }, {
                easing: L.Motion.Ease.easeInOutQuart,
                auto: true,
                duration: 2000,
              }, {
                removeOnEnd: false,
                icon: L.divIcon({
                  html: "<div class='material-icons trackingIcon'>🚚</div>",
                  iconSize: L.point(0, 0)
                })
              }).addTo(this.map);

            this.map.addLayer(path);
            this.mapItems.push(path);

            var start = history[0]; // lila
            var current = history[history.length - 1]; // grün

            this.mapTitle = current.location;

            var greenIcon = new L.Icon({
              iconUrl: 'https://cdn.rawgit.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-green.png',
              shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
              iconSize: [25, 41],
              iconAnchor: [12, 41],
              popupAnchor: [1, -34],
              shadowSize: [41, 41]
            });

            var violetIcon = new L.Icon({
              iconUrl: 'https://cdn.rawgit.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-violet.png',
              shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
              iconSize: [25, 41],
              iconAnchor: [12, 41],
              popupAnchor: [1, -34],
              shadowSize: [41, 41]
            });

            points.forEach((point, i) => {
              const marker = L.marker(point, { autoPan: true });

              const entry = history[i];

              // ugly af monkaS
              var test = history.filter(e => e.location == entry.location && e != entry);

              if (test.every(t => t.updatedAt < entry.updatedAt)) {
                //entry.lat + " " + " " + entry.long + " " + 
                marker.bindTooltip(entry.location + " <br> " + this.formatDateTime(entry.updatedAt), {
                  permanent: true,
                }).openTooltip();

                if (start == entry) {
                  marker.setIcon(violetIcon);
                }

                if (current == entry) {
                  marker.setIcon(greenIcon);
                }

                this.map.addLayer(marker);
                this.mapItems.push(marker);
              }
            });
          });
        }
      },
      getColor(status) {
        switch (status) {
          case "Delivery":
            return "warning";
          case "Transit":
            return "error";
          case "Done":
            return "ok";
          case "Invalid":
            return "critical";
          case "Pickup":
            return "info";
          default:
            return "";
        }
      },
      getIcon(status) {
        switch (status) {
          case "Done":
            return "check_circle_outline";
          case "Transit":
            return "airplanemode_active";
          case "Delivery":
            return "airport_shuttle";
          case "Invalid":
            return "cancel";
          case "Pickup":
            return "error";
          default:
            return "refresh";
        }
      },
      filterHistory(shipment) {
        var tmp = shipment.history.slice().reverse();

        if (shipment == this.expanded) {
          return tmp;
        }
        return tmp.slice(0, 1);
      },
      expand(shipment) {
        if (this.expanded == shipment) {
          this.expanded = null;
        }
        else {
          this.expanded = shipment;
        }
      },
      toggleTheme() {
        this.dark_mode = !this.dark_mode;
      },
      formatHistory(entry) {
        return entry.message;
      },
      formatDateTime(date_string) {
        return new Date(date_string).toLocaleString();
      },
      async deleteShipment(shipment) {
        var result = confirm("Delete shipment " + shipment.trackingId + " ?");
        if (!result) {
          return;
        }

        var result = await fetch("/api/shipments/" + shipment.id, {
          method: "DELETE",
        });

        if (result.status === 200) {
          var index = this.shipments.indexOf(shipment);
          this.shipments.splice(index, 1);
        }
      }
    },
    el: "#app",
    async mounted() {
      setInterval(async () => {
        var result = await fetch("/api/shipments");
        var shipments = await result.json();

        // OMEGALUL LULW LOLW WiredChamp
        for (const shipment of shipments) {
          var index = this.shipments.findIndex(s => s.id == shipment.id);
          if (index != -1) {
            var tmp = this.shipments[index]

            tmp.hasData = shipment.hasData;
            tmp.comment = shipment.comment;

            Vue.set(tmp, "status", shipment.status);
            Vue.set(tmp, "history", shipment.history);
          }
          else {
            this.shipments.push(shipment);
          }
        }

        var outdated = this.shipments.filter(s => shipments.findIndex(s2 => s2.id == s.id) == -1);

        outdated.forEach(shipment => {
          var index = this.shipments.indexOf(shipment);
          this.shipments.splice(index, 1);
        });

      }, 5000);

      var result = await fetch("/api/shipments");
      this.shipments = await result.json();

      console.log(this.shipments);

    },
  });