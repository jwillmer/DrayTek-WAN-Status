# DrayTek-WAN-Status
The app collects the WAN status message that is send via UDP from a [DrayTek modem](https://www.draytek.com/en//products/products-a-z/router.all/vigor130) to your machine and stores it as CSV file or send it to [InfluxDB](https://www.influxdata.com/).


## Usage Options

- Build the app from source code and use the command line: `dotnet "DrayTek WAN Status.dll"`

- Use the command line with docker:

```
docker create \
	--name draytek-log \
	-p 514:51400 \
	-v </path/to/appdata>:/config \
	-v <path/to/logfiles>:/data \
  jwillmer/draytek-wan-status:latest
```


- Use a `docker-compose.yml` file:

```
version: "2"
services:

  draytek_log:
    image: jwillmer/draytek-wan-status:latest
    container_name: "draytek_log"
    volumes:
      - </path/to/logfiles>:/config
      - <path/to/appdata>:/data
    ports:
      - "514:51400/udp"
    restart: always
```

## Configuration

On the first start the app will create a configuration file (`app.config`) and terminate. Please modify this fiel and restart the app.

```js
{
  // Disables the console output if you run it inside a docker container
  "DisableConsoleOutput": false,
  // Disables console print of raw data before processing
  "OutputRawData": false,
  "QueryOptions": {
    // Define the query option, available options: UDP, Telnet
    "Option": "UDP",
    "Udp": {
      // The listening UDP port - do not change this if you run the app inside docker, just map your port to this one.
      "ListeningPort": 51400,
      // IP of your router, to validate that the package is from your router
      "Ip": "192.168.0.1"
    },
    "Telnet": {
      // Username of your outer
      "User": "username",
      // Password of your router
      "Password": "password",
      // The interval (in sec) to query for a new state
      "QueryIntervalSeconds": 30,
      // IP of your router
      "Ip": "192.168.0.1"
    }
  },
  "StorageProvider": {
    // Define where to safe the data, available options: InfluxDb, CSV
    "StorageProviderOption": "CSV",
    "InfluxDb": {
      // The InfluxDB version, available options: Latest, v_1_3, v_1_0_0, v_0_9_6, v_0_9_5, v_0_9_2, v_0_8_x
      "Version": "Latest",
      // InfluxDB URL
      "Url": "http://192.168.0.2:8086",
      // InfluxDB username, empty if none
      "User": "username",
      // InfluxDB password, empty if none
      "Password": "password",
      // InfluxDB database name
      "DatabaseName": "database"
    },
    "Csv": {
      // Delimiter for the CSV file
      "Delimiter": ";"
    }
  }
}
```

## Output

![](https://github.com/jwillmer/DrayTek-WAN-Status/raw/master/media/output-consol.png)

![](https://github.com/jwillmer/DrayTek-WAN-Status/raw/master/media/output-csv.png)
