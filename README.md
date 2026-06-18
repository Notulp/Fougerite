# Fougerite project

## About the Project
Fougerite is a "fork" or conversion of Magma that was created by EquiFox and xEnt, back in 2013 after furnace mod.
The project however became abandoned in 2014, and has been decompiled, and refactored by Riketta.
Later on Alex, balu92, mikec, DreTaX has joined the project, and the mod was renamed from Zumwalt to
Fougerite. On 19th of November, 2014 the contributions have been undertaken by Team Pluton.
Fougerite provides not only a modding platform for the original Rust Legacy Server, but also provides
many bugfixes that were left over by the original developer team.
There is also a client side client available for everyone to use, and develop on called RustBuster.
Fougerite provides C#, Python, Javascript, and Lua language support, and also an easy to use API.

Fougerite a fully compatible with Magma server mod, featuring better performance with Python and C# plugins.

## 12 years of Fougerite
12 Years of Fougerite, A Definitive Legacy What started in 2013 as a conversion of Magma has evolved over the last 12 years into the most stable and comprehensive modding ecosystem for Rust Legacy. While the project was forked by Riketta and later renamed to Fougerite, its true soul was forged through a decade of reverse engineering a game that was originally left with a high volume of subpar code and obsolete technical debt.

Fixing the Foundation The original Rust Legacy code quality left much to be desired, containing leftover obsolete systems that hindered performance and stability. Our mission was to understand the game from the inside out, not just on the server, but deep within the client side architecture. Through years of reverse engineering we did not just add features, we rebuilt the foundation, fixing bugs the original developers left behind and creating a platform that remains the most up to date and well supported in the history of the game.

The Pinnacle of Modding Today, Fougerite stands as the most stable platform with the highest number of hooks and the most dense information community ever achieved for Rust. This journey reached its peak with RustBuster, representing our greatest achievement in client side modding. It allowed us to push the boundaries of what was possible in a game from 2013, providing a level of protection and feature rich development that finally brought out the best in Rust Legacy. You can see the extent of this client side modding in action via our community showcases at https://www.youtube.com/@JuliRust/videos.

The Final Chapter 2025 to 2026 As we hit the 12 year mark in 2026, Fougerite is now considered a complete project, by me, the most active, and original author, DreTaX. Between 2025 and 2026, a final surge of updates was released to officially implement features or implement fixes for every remaining issue on my list. From my early days in 2014 writing simple JavaScript plugins to now, this project has been my primary vehicle for learning the deep arts of reverse engineering high level languages and games. I (we?) leave this project in a semi active state of completion, perfected, polished, and standing as a testament to what a dedicated community can achieve with a game that many thought was dead years ago. Rust Legacy may be a relic of 2013, but Fougerite ensures its best version is preserved forever.

It is hard to believe that over a decade has passed since I first started messing with crappy javascript plugins for Magma back in 2014. This project has been more than just code for me, it was the fire that started my journey into reverse engineering and software development. I spent years refusing to let this game die because I believed the community deserved an open source platform that actually worked. I have spent countless nights staring at decompiled source code, learning how every single packet and function moved within Rust Legacy.

I see Fougerite as complete now. It has everything it needs to stand on its own. While I am moving on to other challenges, my heart stays with the years we spent together in the wasteland. Thank you to everyone who stayed, everyone who tested, and everyone who challenged me to make this better. This is my final goodbye to the active development of Fougerite. I wish I knew then what I know now, but I am proud of the legacy we leave behind. Keep the servers running and keep the community alive. I'll be deploying Rustbuster 3.0 as my time allows for the final time, and then it's time to say farewell.
Goodbye, and thank you for 12 incredible years.

## x64 Upgrade (v1.9.7+)

Starting from version **1.9.7**, Fougerite ships [Fougerite_LibRust_x64](https://github.com/Notulp/Fougerite_LibRust_x64) for testing, a full x64 reimplementation of the original x86 `librust.dll`, alongside x64 builds of `rust_server.exe` and `mono.dll`.

### Setup

**Delete these files from the server root**, they are x86-only Steam redistributables that do not belong in an x64 process:

- `steamclient.dll`
- `steam_api.dll`
- `tier0_s.dll`
- `vstdlib_s.dll`

**Overwrite these files** with the x64 versions from the release:

- `librust.dll`
- `rust_server.exe`
- `mono.dll`

### Why x64 on a 2013 Unity 4.5.5f Engine

- **No 4 GB memory ceiling.** x86 processes are limited to roughly 4 GB of virtual address space. x64 removes that cap entirely, meaning large maps, high player counts, and heavy plugin loads no longer risk out-of-memory crashes that previously required server restarts.
- **More CPU registers.** x64 doubles the number of general-purpose registers over x86. The Mono (Mini) JIT compiler can keep more values in registers rather than spilling to the stack, reducing memory traffic on hot paths like the game loop, plugin execution, and networking.
- **Native on modern operating systems.** Running natively x64 removes the WOW64 compatibility layer that every x86 process runs under on modern Windows, eliminating a layer of overhead on every system call.
- **Better calling convention.** The x64 calling convention passes more arguments in registers rather than on the stack, which is more efficient than the x86 stdcall/cdecl conventions used by the original server.
- **Stability.** Address space exhaustion is one of the leading causes of long-running Rust Legacy server crashes.
- **Future compatibility.** x86 tooling, runtimes, and OS support will continue to erode. The x64 build ensures Fougerite can run on whatever comes next without requiring a compatibility update hopefully for the next 20 years..

---

## Compilation
1. First you need to decide wheather you are going to modify the patcher or not. If you are only here to modify or compile
   the Fougerite project, or one of the engines skip to step 7.
2. Open the SLN, and compile the Fougerite patcher.
3. Please go to the Fougerite\References\CleanPatchTargetDlls\ directory, and read the ReadMe.txt file.
4. Select the 3 dlls that you are going to patch (Assembly-CSharp.dll, uLink.dll, Facepunch.MeshBatch.dll)
   , and copy to the patcher's output directory. You may need other files as reference
   such as UnityEngine.dll
5. Run the patcher, and enter 0. If all is well, then the dlls that you have copied to the directory are now patched.
   If something went wrong, try to find out from the patcher's logs.
6. Copy the 3 patched dlls to \Fougerite\References\PatchedRustDlls\ directory, and overwrite the existing ones.
7. You can now build all the projects using JetBrains Rider, or Visual Studio as you like.

## Installation
Please see a release file from the releases tab to see how a release file should look like.
The release file also contains extra Python files, that you may want to grab.
It's in the Save\Lib folder. Not all of the python files can be used due to the version of
mono or IronPython. The issue has been resolved on a newer IronPython version, however when I tried It
It didn't work. (The issue page doesn't exist, they moved to github.)
Once you have copied all of your files accordingly, you may run your server. (You also have to overwrite some server files)
There is also a clean steam server available on this repository for you to download.

Use Git Issues system to report bugs, please.
Please visit [our forum](http://fougerite.com/) for more information.

[![Watch the video](https://img.youtube.com/vi/FHjaZjCdfLI/maxresdefault.jpg)](https://youtu.be/FHjaZjCdfLI)

[Join our Discord Server](https://discord.gg/fNsBMeHW)

***
###### Developed by EquiFox & xEnt (Rust++ and Magma)
###### Forked by Riketta (Zumwalt Project)
###### Renamed by Alexknvl (from "Zumwalt" to "Fougerite")
###### 19-NOV-2014: Contributions and on-going maintenance undertaken by Team Pluton
