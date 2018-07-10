# PocketSphinx KWS C#
Pure C# port of the Pocketsphinx keyword spotter

This code is terribly unoptimized right now because it's an LLE (low-level) port of the C code, favoring parity over speed. It's about 7 times slower than the C code right now; future optimizations to remove the pointer emulation layer should come soon.

Also TODO:
- Define the external API
- Add an interface for those who only want speech detection
- Push to Nuget
