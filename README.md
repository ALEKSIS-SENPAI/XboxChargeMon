<img width="140" height="88" alt="9x3bpzb" src="https://github.com/user-attachments/assets/6ce81f15-a4cc-44de-8645-d5a4a847a5d3" />
<img width="140" height="88" alt="9x3bpzb" src="https://github.com/user-attachments/assets/2ace348d-796c-4c2a-9afc-6706ad8db451" />


# XboxChargeMon

Xbox controller battery in your tray. That's it.

## Why

Microsoft put USB-C on the 1914 and removed the LED from the charging cable for some reason. So now checking battery means opening Game Bar, which is a whole overlay when you've got a second monitor and just want a glance. This doesn't do that.

Also XInput has no charging flag. Never did. So detecting charging means watching the battery level go up over time and going "okay probably charging then." It's not great but that's what we have.

## Requirements

- Windows 10 or 11
- .NET Framework 4.x (already on your machine, don't worry about it)
- Xbox controller with the Xbox Wireless Adapter. Bluetooth might work, not my setup so can't say for sure.

## Getting it

Pre-built EXE on the [Releases](../../releases) page if you just want to run it.

Source is here as-is, run `build.bat` and it'll compile everything into a standalone EXE. Uses csc.exe which already ships with Windows so no installs needed.

## Pinning to the tray

Windows 11 shoves new tray icons behind the little arrow. Drag it out to the main bar or you'll never see it.

## Usage

Hover for battery level and a rough time estimate. Double-click to pop a status balloon. Right-click for startup toggle and to exit.

## Tray states

```
Xbox | Connected | Reading...         Found it, waiting on battery data
Xbox | Full      | ~15-20h
Xbox | Medium    | ~8-12h
Xbox | Medium (rising) | ~8-12h      Level went up, probably charging
Xbox | Low       | ~2-4h             Sends a notification here
Xbox | USB connected                  Plugged into the PC directly
Xbox | No controller
```

## Custom icons

Drop 32x32 PNGs into an `icons\` folder next to the EXE, they'll override the embedded ones:

```
disconnected.png  full.png  medium.png  low.png  wired.png
```

## Notes

- XInput doesn't report a percentage, it gives 4 states. Working with what I've got
- Time estimates are rough, based on a standard rechargeable pack
- Charging only shows up when the level actually ticks up so it can take a while
- Scans all four XInput slots, grabs the first active one

## Prior art

NiyaShy's [XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator) does basically the same thing and has been around way longer. Not trying to copy it, just wanted my own take built around my setup. Probably a better dev than me too.

## License

MIT
