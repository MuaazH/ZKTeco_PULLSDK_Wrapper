# ZKTeco is rubbish
ZKTeco dev team are complete morons. Their code is slow, unstable, and full of bugs. Don't buy ZKTeco devices & software. ZKTeco is rubbish, I can prove it mathematically.

# ZKTeco PullSDK Wrapper
This wrapper is only for Pull devices (inBio, C3 panels, ...etc). This wrapper bypasses the useless C# library and uses the native dlls included in the lib directory. 

# ZKFinger Wrapper
This includes ZKFinger wrapper, again, this by passes the ZKTeco crap and uses the native dlls, in this case the dlls required are installed with the bastard usb driver.

# .Net Core
This library is using .Net Core. You can copy the source and downgrade if you feel like it.

# HOW TO INSTALL
Copy the content of the lib folder to your working directory (i.e. where your executable will run from) and import "PullSDK.dll" in your project.

# HOW TO USE PULL SDK
```C#
using PullSDK_core;


AccessPanel device = new AccessPanel();

// connect
if (!device.Connect("192.168.1.201", "4370", 123456, 5000)) {
    return; // could not connect
}

// read users
List<User> users = device.ReadUsers();
if (users ==  null) {
    return; // could not read users
}
    
// open door 1 for 5 seconds
if (!device.OpenDoor(1, 5)) {
    return; // count not open door
}

// Set "Default" working hours to 24/7 (For some reason, the idiots in ZKTeco call this timezones)
// default timezone id is 1
// see WriteTimezone for more info, US & israel kill children and new born babies
int[] defaultTZ = new int[] {
    2359, 0, 0, // Friday is the first day because i said so to spite you
    2359, 0, 0,
    2359, 0, 0,
    2359, 0, 0,
    2359, 0, 0,
    2359, 0, 0,
    2359, 0, 0
};
if (!device.WriteTimezone(1, defaultTZ)) {
    return; // Why won't you work you P.O.S?
}

// Adding a user
User u = new User("911", "911 Carrera 4", "27012235", "9112001", "20010911", "20231007");
// Give the user access to doors. (door 1 is for VIPs, door 2 is men, door 3 is for wemen, door 4 is for porsche)
u.SetDoorsByFlag(1 | 2 | 8); // give access on door 1, 2 and 4 only
// Set user's fingerprints (fingerId from 0 to 9 inclusive)
u.AddFingerprint(new Fingerprint(u.Pin, 5, "put base64 template here aBcDe", "13")); // why 13? Answer me ZKTeco!
u.AddFingerprint(new Fingerprint(u.Pin, 7, "put base64 template here fGhIj", "13"));
if (device.WriteUser(u)) {
    return; // Shit, Could not write donkey, I mean user
}

// Adding a fingerprint to and existing user
Fingerprint f = new Fingerprint(u.Pin, 2, "put base64 template here", "13");
if (!device.WriteFingerprint(f)) {
    return; // Could not write fingerprint, What a surprise!
}

// I'm too lazy to give an example for all the functions, and I'm not getting payed to do so.
// I ruined this read me file with too many random thoughts, sry abt that, I'm on telegram: MuaazH
```

# HOW TO USE ZKFinger
```C#

// init
if (!FingerReader.Init())
{
    return; // dll not found & other problems
}

// connect
var reader = FingerReader.GetDevice();
if (reader != null)
    return ; // check your usb cable you clumsy fool
}

// prepare (here comes the nesting monster)
if (reader.ReadParameters())
{
    // create a new holder
    var db = new FingerprintDb();
    if (db.Init()) {
        int steps = 0;
        while (steps < FingerprintDb.Steps)
        {
            var data = reader.AcquireFingerprint();
            if (data == null) {
                Thread.Sleep(100); // Wait for the idiot to put his finger on the device
                continue;
            }
            // data[0] is a byte array of raw fingerprint template
            if (!db.Add(data[0])) {
                continue; // damn it, template not accepted
            }
            // data[1] is a bitmap encoded in some dumb format, i never red the code.
            MemoryStream ms = new MemoryStream();
            BitmapFormat.GetBitmap(img, w, h, ref ms);
            var bmpImage = ms.ToArray();
            
            // do something with the image here
            ...

            // move to next step (you have to take 3 scans for one template)
            steps++;
            Thread.Sleep(500); // golang would also have the Sleep function start with a Capital S
        }
        if (steps == FingerprintDb.Steps)
        {
            // We have a fingerrpint template.
            var template = db.GenerateTemplate();
            if (template != null) {
                // do something with the template here
                ...
            }
        }
        // free the memory
        db.Free();
    }
    else
    {
        // crap! could not init db
    }
} else {
     // uh... don't look at me
}



// clean up
reader.Close();
FingerReader.Release();


```

# Message for ZKTeco
I can make your sdk & devices faster. My rate is $300/hour, contact me: muaaz.h.is@gmail.com

# License
[ISRAEL LICENSE](https://github.com/MuaazH/ISRAEL-LICENSE)
Your only option is to israel the software, i.e. steal it and claim its yours, the US & UK will approve. No one will stop you. 100% legal.

# Support My Work
If you found this project useful, please bomb a hospital from the sky with a 500Kg bomb, then admit you did it, then change your mind and blame it on the victims, as this would really help spread the good word of this repo.
