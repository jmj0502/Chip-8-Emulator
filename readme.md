# CHIP-8 Emulator
This is a basic CHIP-8 emulator written in C#. Why C# you've may asked? Because it's a compiled language with static typing
and it's kind of "modern" in comparison to C/C++ (please don't ditch me by this) and I'm not that experienced working with those languages.

## How to run the project?
This project is technically configured so you can run it without that much of a troubble. The first thing you need to do
is install [SdlDotNet](https://www.nuget.org/packages/SdlDotNet/ "SdlDotNet on Nuget") (the package with less documentation I've seen until now) via Nuget, once you've done so, point 
the dependecy on our solution to its global installation and should be able to use it without a trouble. 

Another thing to keep in mind, is that SdlDotNet present different issues if you try to compile it for x64 arquitechtures, so 
before you try and compile this project, make sure that you're targeting a x86 arquitechture on your project configuration.

## Is this project done?
No, it ain't. However, you can run it and even play games with it, there are different UI/UX factors I want to improve
but in general this works just fine.

## How did you learn to build this thing?
Ok, this is more a "technical references" section than anything else. The first thing you need to understand is how to
read **binary** and **hexadecimal** (this may seem imposible at the beginning but believe me it's not that complicated once you've get started)
and how to perform addition, subtraction, shift and bitwise operations with them. Once you got all those things down,
you need to consult a CHIP-8 technical reference; the most popular and the most well documented one is CowGod's Chip-8 
reference (I'll add a link to this a few lines below), another good source of information is the Chip-8 page on Wikipedia.

When it comes to the code implementation of the whole project, the best thing you can do is to find as much references
as you can. Surf the web until you've seen enough implementations of Chip-8 emulators on the language you want to use, that
will allow you to comprenhend how to deal with certain scenarios (ex: how to relate variables with chip-8 components, etc).

## References
[CowGod's Chip-8 technical reference](http://devernay.free.fr/hacks/chip8/C8TECH10.HTM "Cowgod's tecnical reference.")
[Wikipedia page on Chip-8](https://en.wikipedia.org/wiki/CHIP-8 "Excelent complementary guide")
[A medium article on how to read binary](https://medium.com/@LindaVivah/learn-how-to-read-binary-in-5-minutes-dac1feb991e)
[An article on how to read and write hex](https://learn.sparkfun.com/tutorials/hexadecimal/all#:~:text=Start%20with%20the%20right%2Dmost,%2C%2014%2C%20and%2015)