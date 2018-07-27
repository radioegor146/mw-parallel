# mw-parallel
Master-worker parallel task system written on C#

# Deployment the system
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
        All other sections are inactive and I recommed to set them as in default config.
        
