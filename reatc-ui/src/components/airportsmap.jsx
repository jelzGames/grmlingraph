import React, { useRef, useEffect, useState } from 'react';
import { LoadScript, GoogleMap } from '@react-google-maps/api';

const containerStyle = {
  width: '100vw',
  height: '100vh'
};


const api = "https://localhost:7049";
const libraries = ['marker']; 

const MyMap = () => {
  const [mapInstance, setMapInstance] = useState(null);
  const markersRef = useRef([]);
  const flightPathRef = useRef([]);
  const [countries, setCountries] = useState([]);
  const [selectedCountry, setSelectedCountry] = useState("Finland");
  const [center, setCenter] = useState({ lat: 64.0, lng: 26.0 }); 
  

  useEffect(() => {
    const fetchCountries = async () => {
      const res = await fetch("https://raw.githubusercontent.com/mledoze/countries/master/countries.json");
      const data = await res.json();
      const countryList = data
        .filter(c => Array.isArray(c.latlng) && c.latlng.length === 2)
        .map(c => ({
          name: c.name.common,
          center: {
            lat: c.latlng[0],
            lng: c.latlng[1],
          },
        }))
        .sort((a, b) => a.name.localeCompare(b.name));
      setCountries(countryList);

    };

    fetchCountries();
  }, []);

  useEffect(() => {
    const match = countries.find(c => c.name === selectedCountry);
    if (match) {
      setCenter(match.center);
      loadData();
    }
  }, [selectedCountry, countries]);


  const addEvent = (marker, map) => {
    marker.addListener('click', () => {


      const url = `${api}/Airports/GetDestinations?orign=${marker.iata}`;
      fetch(url)
        .then((res) => res.json())
        .then((data) => {
          if (flightPathRef.current) {
            flightPathRef.current.forEach(line => line.setMap(null));
            flightPathRef.current = [];
          }
          data.forEach((airport) => {
            const datalat = airport.properties.find(x => x.key === "lat")?.value;
            const datalog = airport.properties.find(x => x.key === "lon")?.value;
      
            const lat = parseFloat(datalat);
            const lng = parseFloat(datalog);
            const latlong = { lat, lng };
            const coords = [marker.latlong, latlong];

           
            let path = new window.google.maps.Polyline({
              path: coords,
              geodesic: true,
              strokeColor: 'blue',
              strokeOpacity: 1.0,
              strokeWeight: 2
            });

            path.setMap(map);
            flightPathRef.current.push(path);
          });
        });
    });
  };

  useEffect(() => {
    if (!mapInstance || !window.google?.maps?.marker?.AdvancedMarkerElement) return;

  
    let url = `${api}/Airports/LoadData?country`;

    fetch(url)
      .then(async(res) => {
         
      });
  }, [mapInstance]);

  const loadData = async () => { 
    let url = `${api}/Airports/LoadData?country=${selectedCountry}`;

    fetch(url)
      .then(async(res) => {
         await FindAirports();
      });
  }

  const FindAirports = async () => { 
    let url = `${api}/Airports/GetAll`;

    fetch(url)
      .then(res => res.json())
      .then(data => {
        data.forEach(async(airport, index) => {
          const datalat = airport.properties.find(x => x.key === "lat")?.value;
          const datalog = airport.properties.find(x => x.key === "lon")?.value;
          const dataiata = airport.properties.find(x => x.key === "iata")?.value;
          const datacity = airport.properties.find(x => x.key === "city")?.value;

          const lat = parseFloat(datalat);
          const lng = parseFloat(datalog);
          const latlong = { lat, lng };

          if (index === 0) {
            mapInstance.setCenter(latlong);
            mapInstance.setZoom(5);
          }

          const { Marker } = await window.google.maps.importLibrary("marker");

           const marker =  new Marker({
            map: mapInstance,
            position: latlong,
            title: `${dataiata} -- ${datacity}`
          });

          marker.iata = dataiata;
          marker.latlong = latlong;

          addEvent(marker, mapInstance);
          markersRef.current.push(marker);
        });
      });
  }

  return (
    <div>
      <div style={{ paddingBottom: 20 }}>
        <select 
          value={selectedCountry}
          onChange={(e) => setSelectedCountry(e.target.value)}
          className="custom-select"
        >
          {countries.map((country) => (
            <option key={country.name} value={country.name}>
              {country.name}
            </option>
          ))}
       </select>
      </div>
      <LoadScript googleMapsApiKey="your_api_key_from google" libraries={libraries}>
        <GoogleMap
          mapContainerStyle={containerStyle}
          center={center}
          zoom={4}
          mapId="your_map_id" 
          onLoad={map => setMapInstance(map)}
        />
      </LoadScript>
     </div>
   
  );
};

export default MyMap;
