# ZKTeco PullSDK Wrapper
ZKTeco is a really really bad company. DO NOT BUY ITS DEVICES. THEY ARE JUNK.

  ZKTeco programmers are so BAD. They made an sdk that neither: hides how the device works nor give you full control over the device.
  On top of that, they can't speak english for $#!t. I swear google translate can score better than all of them combined in the ESL test.
  The end result is a security device that can be hacked with a magnet or a paper clip, a verbose and unencrypted network protocol, incomplete and inconsistent documentation, and an SDK that only runs on windows.

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
    
// open door 1 for 5000 ms
if (!ACDevice.OpenDoor(1, 5000)) {
    return; // count not open door
}
```
# Expert opinion: DO NOT BUY ZKTeco devices
It's just a bad company.

# Why am I supporting their SDK then, am I stupid?
N.. No NOOOO! shut up! your stupid. I mean you're stupid. It did it for a job, Someone had to do the dirty work. It's a victimless crime relative to what ZKTeco did.

# Message for ZKTeco
Get good you clowns, learn what a data structure is, Learn programming, and learn the englich.
If you want to hire me to fix you're mess (See that was a test, your English is still bad). My rate is $250/hour, contact me: muaaz.h.is@gmail.com

# License
ISC or your driver's license.

# Support My Work
If you found this project useful, Please pretend to be an investor and contact ZKteco to try to scam them using the pyramid scheme, as this would really help spread the good word of this repository.
