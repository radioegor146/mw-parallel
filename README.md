# mw-parallel
Master-worker parallel task system written on C#

# Manual deployment of the system
All you need is just source files
1. Clone or download this repository
2. Now you need to compile Master and start it:
    1. Compile it
    2. Make your own config.json file:
        ```JSON
        {
            "wsport": 6969,
            "mainport": 9696,
            "password": "default",
            "datacontroller": "Master.DataControllers.SimpleDataController",
            "defaultpermission": 5,
            "controllercfg": {
                "accounts": [{
                    "password": "default",
                    "permissions": 5
                }]
            }
        }
        ```
        **wsport**: port for cpanel to connect to;  
        **mainport**: port for Workers to connect to;  
        **password**: password for Workers to use;  
        **datacontroller**: classname of used datacontroller class (only SimpleDataController is avaible now)  
        All other sections are inactive and I recommend to set them as in default config.  
3. Third thing is to setup Workers on your PCs
    1. Create a class that inherits interface IMainVoid
    2. Compile Worker
    3. Make config.json file for all Workers
        ```JSON
        {
            "wsaddr": "ws://127.0.0.1:9696",
            "password": "default",
            "mainvoid": "Worker.MainVoid"
        }
        ```
        **wsaddr**: Master host for Worker to connect to;  
        **password**: password for Workers to use;  
        **mainvoid**: classname of used class of IMainVoid class  
4. Run Master and all your Workers
5. Open CPanel HTML file and edit these lines of config:
    ```HTML
    ...
    <script>
        var config = {
            infoaddr: "ws://127.0.0.1:6969", //set this line to your Master CPanel host
        }
    </script>
    ...
    ```
6. It's done! Now your system is ready to use!
