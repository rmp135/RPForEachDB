# RPForEachDB

Windows tool for running SQL scripts over multiple databases of an Microsoft SQL Server.

## Usage

- Add a server in the Manage Servers screen. 
- Select your server in the dropdown on the main screen and click Get Databases.
- Select databases on the left.
- Type your SQL (or load a file) in the top right textbox.
- Click Run.

All selected databases will have the script ran asynchronously. If messages or errors are returned they will be displayed in the bottom right textbox.

The script is ran split by `GO` statements. These can be used to track progress of a running script which will be shown in the database grid.

![Main App](https://i.imgur.com/RUMQOB1.png)

![Connection Manager](https://i.imgur.com/QU2GmFj.png)

## Notes

Scripts are not ran in transactions and therefore errors must be handled by the script itself.

SQL Server authentication usernames and passwords are stored in plain text. Please do not use this option if it can avoided. 

Configuration files are saved in one of the `%userprofile%\AppData\` folders under the sudirectory RPForEachDB. 

## Disclaimer

All efforts have been made to ensure this program runs smoothly without adverse effects. The author takes no responsibility from any damage caused by the use of this program.