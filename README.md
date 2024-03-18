# Imperfect Gamers [IG] - Activity Tracker

A plugin for user activity tracking for CounterStrikeSharp.

---

## Requirements
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [MySQL](https://www.mysql.com/) database to save information

---

## Installation
- Download the latest release from [here](https://github.com/razpbrry/Imperfect-ActivityTracker/releases)
- Unzip and place into your servers `game/csgo/` directory

---

## Configuration

After installation and initial run of the plugin, a configuration file will be created in the `game/csgo/addons/counterstrikesharp/configs/plugins/ImperfectActivityTracker/` directory.


### Database Settings - Required

`DatabaseHost` - The IP/Hostname for your database

`DatabasePort` - Port for your database (For exmaple MySQL 3306 for MySQL)

`DatabaseName` - Database name to save information to.

`DatabaseUser` - Username to login to your database

`DatabasePassword` - Password for username to login to your database

### Server Information

`ServerIp` - The IP for the server that this is being installed on. This is to save unique time data related to this specific server.

---

## Credits
The time tracking module within the [K4-System](https://github.com/K4ryuu/K4-System) plugin inspired and provided the foundation for the code found in this repository.
