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
using i04PullSDK;


AccessPanel ACDevice = new AccessPanel();

// connect
if (!ACDevice.Connect("192.168.1.201", "4370", "1234", 5000)) {
    return; // could not connect
}

// read users
List<User> users = ACDevice.ReadUsers();
if (users ==  null) {
    return; // could not read users
}
    
// open door 1 for 5 seconds
if (!ACDevice.OpenDoor(1, 5)) {
    return; // count not open door
}
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
ISC or your driver's license.

# Support My Work
If you found this project useful, Please pretend to be an investor and contact ZKteco to try to scam them using the pyramid scheme, as this would really help spread the good word of this repository.
