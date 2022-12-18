# OCB Startup Optimizer - 7 Days to Die (A20) Addon

This mod will re-compress any LZMA unity3d resource to use LZ4 compression for faster load times.
The LZ4 format will probably use 20% to 80% more diskspace than LZMA compression.
But it is virtually free in terms of decompression, while LZMA adds a significant overhead there.
LZMA decompression is slow at maybe 20MB/s, while LZ4 will go up to 5GB/s.

Reports indicated good results on Undead Legacy, Darkness Falls and other overhauls.
Note that in the future those mods will probably use that LZ4 "speed" trick directly.
Until then, this mod can optimize assets bundles for overhauls and single mods.

Please Note that on first run, the optimizer will make the game unresponsive.
Give it a few minutes in order to finish the optimization step.
And EAC must be disabled in order for it to work.


Mod is proven to be able to optimize DF and UL assets/resources.
Apparently it (unexpectedly) also helps dedicated server startup times.

Currently the optimizer will keep the original unity3d files for safety measures.
Once you've verified everything is working correctly, you can remove "*.org" files.
To do that there is a console command `optimizer cleanup` you can use to do that.

[![GitHub CI Compile Status][4]][3]

## Download and Install

Simply [download here from GitHub][2] and put into your A20 Mods folder:

- https://github.com/OCB7D2D/OcbStartupOptimizer/archive/master.zip (master branch)

## Changelog

### Version 0.2.0

- Add `optimizer cleanup` command

### Version 0.1.1

- Add support for Darkness Falls
  Also checks `Assets` folders

### Version 0.1.0

- Initial version

## Compatibility

I've developed and tested this Mod against version a20.6(b9).

[1]: https://github.com/OCB7D2D/OcbStartupOptimizer
[2]: https://github.com/OCB7D2D/OcbStartupOptimizer/releases
[3]: https://github.com/OCB7D2D/OcbStartupOptimizer/actions/workflows/ci.yml
[4]: https://github.com/OCB7D2D/OcbStartupOptimizer/actions/workflows/ci.yml/badge.svg
