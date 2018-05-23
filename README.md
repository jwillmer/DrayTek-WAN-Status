# DrayTek-WAN-Status
The app collects the WAN status message that is send via UDP from a [DrayTek modem](https://www.draytek.com/en//products/products-a-z/router.all/vigor130) to your machine and stores it as CSV file or send it to [InfluxDB](https://www.influxdata.com/).


## Usage Options

- Build the app from source code and use the command line: `dotnet "DrayTek WAN Status.dll"`

- Use the command line with docker
```
docker create \
	--name draytek-log \
	-p 514:51400 \
	-v </path/to/appdata>:/config \
	-v <path/to/logfiles>:/data \
  jwillmer/draytek-wan-status
```

- Use a `docker-compose.yml` file
```
version: "2"
services:

  draytek_log:
    image: jwillmer/draytek-wan-status
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
  // The listening UDP port - do not change this if you run the app inside docker, just map your port to this one.
  "ListeningPort": 51400,
  // IP of your router, to validate that the package is from your router
  "DrayTekIp": "192.168.0.1",
  // Define where to safe the data, available options: InfluxDb, Disk
  "StorageProvider": "InfluxDb",
  // The InfluxDB version, available options: Latest = 0, v_1_3 = 1, v_1_0_0 = 2, v_0_9_6 = 3, v_0_9_5 = 4, v_0_9_2 = 5, v_0_8_x
  "InfluxDbVersion": "Latest",
  // InfluxDB URL
  "InfluxDbUrl": "http://192.168.0.2:8086",
  // InfluxDB username, empty if none
  "InflucDbUser": "",
  // InfluxDB password, empty if none
  "InfluxDbPassword": "",
  // InfluxDB database name
  "InfluxDbDatabaseName": "vigor_130"
}
```

## Output

![](/media/output-consol.png)

![](/media/output-csv.png)
