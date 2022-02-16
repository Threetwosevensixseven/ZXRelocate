# ZXRelocate
Tool to generate relocation tables for ZX Spectrum Next™ drivers and other Z80 asm programs.

## Introduction
[ZX Spectrum Next](https://www.specnext.com/about/)™ [drivers](https://gitlab.com/thesmog358/tbblue/-/tree/master/src/asm/sample_prt) are short pieces or Z80 code loaded by [NextZXOS](https://gitlab.com/thesmog358/tbblue/-/tree/master/docs/nextzxos), the ROM operating system of the Next. Because there are four driver slots in the Next [divMMC](https://spectrumforeveryone.com/features/history-esxdos-divmmc-divmmc-enjoy/) RAM, allowing up to four drivers to be installed at the same time, the driver code must be relocatable. To achieve this, the driver format specification includes two data structures:

* A count of the number of 16-bit address offsets that must be patched for relocation (between 0 and 255);
* A list of the 16-bit address offsets themselves, organised as contiguous little-endian byte pairs. Each offset points to the _high_ byte of a 16-bit little-endian address to be patched. Because driver files are exactly 512 bytes long, the high bytes of these addresses will always be `0x00` or `0x01` before patching, and the driver installation routine will give a `Bad Relocation Table` error if any other values are encountered.

In order to generate driver files in an assembler, typically all the absolute addresses in the driver (the targets of jumps, calls, and 16-bit loads) must be patched. This quickly gets tedious during development, while the code you are writing is moving around. So it is advantageous to have a tool which will take care of this for you.

## Installation
Install the [.NET Framework 4.8  Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer) if you do not already have it installed. Copy the files `ZXRelocate.exe` and `ZXRelocate.exe.config` into a directory, such as the one that contains your build script. Edit your build script to execute `ZXRelocate.exe`. Typically a driver will be assembled twice, since it contains self-referential data (although it could be managed in a single assembly pass using displacements). Therefore, add it to your build script between the first and second assembly. Have the first assembly export a symbol list, which `ZXRelocate.exe` will refer to. Have your assembly project include two assembly files - `RelocationTable.asm` which will contain up to 255 pairs of bytes, and `RelocationCount.asm`, which will define a single symbol that can be referenced in the driver header.

## Config File
The `ZXRelocate.exe.config` file looks like this:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="SymbolFile" value="..\data\Test.sym" />
    <add key="SymbolRegEx" value="RE_.*?\sEQU\s(?&lt;Address>.*)" />
    <add key="AddressOffset" value="1" />
    <add key="DefineWord" value="dw " />
    <add key="Equate" value=" EQU " />
    <add key="Comment" value="; " />
    <add key="IncludeComments" value="true" />
    <add key="RelocateTableFile" value="..\data\RelocateTable.asm" />
    <add key="RelocateCountFile" value="..\data\RelocateCount.asm" />
  </appSettings>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
</configuration>
```
**SymbolFile**: this is the path to the symbol list file exported by your driver project. It must already exist before you run `ZXRelocate.exe`.

**SymbolRegEx**: this is an [XML-escaped](https://www.freeformatter.com/xml-escape.html) [regular expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions) that matches the labels for the symbols to be relocated, and captures the 16-bit value into a `(?<Address>.*)` [named capture group](https://www.regular-expressions.info/named.html) with the name `Address`. In the example here, we are matching all labels containing `RE_`, followed by `EQU`, followed by the address. This is a reasonably common symbol list format, so you may not need to change the regex.

**AddressOffset**: this is an integer number (between `-65535` and `65535`). Because Next drivers patch the _high_ byte of each offset, we need to set this value to `1`, to add `+1` to the symbol which is typically pointing at the low byte in most assembly source.

**DefineWord**: this is the directive your assembler uses to define 16-bit words. `dw ` or `defw` are typical values that work in most assemblers.

**Equate**: this is the directive your assembler uses to define immutable equates. ` EQU` or `=` are typical values that work in most assemblers.

**Comment**: this is the directive your assembler uses to prepend single-line comments. `; ` or `// ` are typical values that work in most assemblers.

**IncludeComments**: set this to `true` if you want to include helpful comments in the generated `.asm` files. Set it to `false` if you don't want comments or they are causing your assembler difficulties.

**RelocateTableFile**: this is the path to the `.asm` file to be generated by list exported by `ZXRelocate.exe`. Include this in your driver file immediately after the 512 byte null-padded driver.

**RelocateCountFile**: this is the path to the `.asm` file to be generated by list exported by `ZXRelocate.exe`. Include this in your driver header assembly source, and emit the count as a single byte in the 6th byte of the header, e.g. `db RelocateCount`.

## Example Symbol File
Here is a short section of a symbol file, containing three `RE_` symbols that will match the regex:

```
check_printer                   EQU $0036
OpenESPConnection               EQU $003C
OpenESPConnection.RE_Ok         EQU $003D
OpenESPConnection.pexit         EQU $0046
RE_Fred                         EQU $0072
RE_Foo123                       EQU $0055
Commands                        EQU $0046
```

## Example Relocate Table File
Here is a relocate table file generated from the test data in this repository:

```asm
; RelocateTable.asm
; Generated automatically by Relocate.exe

dw $003E ; OpenESPConnection.RE_Ok         EQU $003D
dw $0073 ; RE_Fred                         EQU $0072
dw $0056 ; RE_Foo123                       EQU $0055
```

## Example Relocate Count File
Here is a relocate count file generated from the test data in this repository:

```asm
; RelocateCount.asm
; Generated automatically by Relocate.exe

RelocateCount EQU 3 ; Relocation table is 6 byte(s) long
```
## Copyright and Licence
ZXRelocate is copyright © 2022 Robin Verhagen-Guest, and is licensed under [GPL-3](https://github.com/Threetwosevensixseven/ZXRelocate/blob/main/LICENSE).

ZX Spectrum Next is a trademark of SpecNext Ltd.
