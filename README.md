# ZKTeco PullSDK Wrapper
This wrapper is only for Pull devices (inBio, C3 panels, ...etc). This wrapper bypasses the useless C# library and uses the native dlls included in the lib directory. 

# .Net Core
This libray is using .Net Core. You can copy the source and downgrade if you feel like it.

# HOW TO USE
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

# Message for ZKTeco
I can make your sdk & devices faster. My rate is $300/hour, contact me: muaaz.h.is@gmail.com

# License
ISC or your driver's license.

# Support My Work
If you found this project useful, Please pretend to be an investor and contact ZKteco to try to scam them using the pyramid scheme, as this would really help spread the good word of this repository.
