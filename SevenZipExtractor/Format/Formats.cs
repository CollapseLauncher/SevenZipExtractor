using SevenZipExtractor.Enum;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
// ReSharper disable UseCollectionExpression

namespace SevenZipExtractor.Format
{
    public static class FormatIdentity
    {
        internal static FrozenDictionary<SevenZipFormat, Guid> GuidMapping = new Dictionary<SevenZipFormat, Guid>
        {
            {SevenZipFormat.SevenZip, "23170f69-40c1-278a-1000-000110070000".ParseAsGuid()},
            {SevenZipFormat.Arj,      "23170f69-40c1-278a-1000-000110040000".ParseAsGuid()},
            {SevenZipFormat.BZip2,    "23170f69-40c1-278a-1000-000110020000".ParseAsGuid()},
            {SevenZipFormat.Cab,      "23170f69-40c1-278a-1000-000110080000".ParseAsGuid()},
            {SevenZipFormat.Chm,      "23170f69-40c1-278a-1000-000110e90000".ParseAsGuid()},
            {SevenZipFormat.Compound, "23170f69-40c1-278a-1000-000110e50000".ParseAsGuid()},
            {SevenZipFormat.Cpio,     "23170f69-40c1-278a-1000-000110ed0000".ParseAsGuid()},
            {SevenZipFormat.Deb,      "23170f69-40c1-278a-1000-000110ec0000".ParseAsGuid()},
            {SevenZipFormat.GZip,     "23170f69-40c1-278a-1000-000110ef0000".ParseAsGuid()},
            {SevenZipFormat.Iso,      "23170f69-40c1-278a-1000-000110e70000".ParseAsGuid()},
            {SevenZipFormat.Lzh,      "23170f69-40c1-278a-1000-000110060000".ParseAsGuid()},
            {SevenZipFormat.Lzma,     "23170f69-40c1-278a-1000-0001100a0000".ParseAsGuid()},
            {SevenZipFormat.Nsis,     "23170f69-40c1-278a-1000-000110090000".ParseAsGuid()},
            {SevenZipFormat.Rar,      "23170f69-40c1-278a-1000-000110030000".ParseAsGuid()},
            {SevenZipFormat.Rar5,     "23170f69-40c1-278a-1000-000110CC0000".ParseAsGuid()},
            {SevenZipFormat.Rpm,      "23170f69-40c1-278a-1000-000110eb0000".ParseAsGuid()},
            {SevenZipFormat.Split,    "23170f69-40c1-278a-1000-000110ea0000".ParseAsGuid()},
            {SevenZipFormat.Tar,      "23170f69-40c1-278a-1000-000110ee0000".ParseAsGuid()},
            {SevenZipFormat.Wim,      "23170f69-40c1-278a-1000-000110e60000".ParseAsGuid()},
            {SevenZipFormat.Lzw,      "23170f69-40c1-278a-1000-000110050000".ParseAsGuid()},
            {SevenZipFormat.Zip,      "23170f69-40c1-278a-1000-000110010000".ParseAsGuid()},
            {SevenZipFormat.Udf,      "23170f69-40c1-278a-1000-000110E00000".ParseAsGuid()},
            {SevenZipFormat.Xar,      "23170f69-40c1-278a-1000-000110E10000".ParseAsGuid()},
            {SevenZipFormat.Mub,      "23170f69-40c1-278a-1000-000110E20000".ParseAsGuid()},
            {SevenZipFormat.Hfs,      "23170f69-40c1-278a-1000-000110E30000".ParseAsGuid()},
            {SevenZipFormat.Dmg,      "23170f69-40c1-278a-1000-000110E40000".ParseAsGuid()},
            {SevenZipFormat.XZ,       "23170f69-40c1-278a-1000-0001100C0000".ParseAsGuid()},
            {SevenZipFormat.Mslz,     "23170f69-40c1-278a-1000-000110D50000".ParseAsGuid()},
            {SevenZipFormat.PE,       "23170f69-40c1-278a-1000-000110DD0000".ParseAsGuid()},
            {SevenZipFormat.Elf,      "23170f69-40c1-278a-1000-000110DE0000".ParseAsGuid()},
            {SevenZipFormat.Swf,      "23170f69-40c1-278a-1000-000110D70000".ParseAsGuid()},
            {SevenZipFormat.Vhd,      "23170f69-40c1-278a-1000-000110DC0000".ParseAsGuid()},
            {SevenZipFormat.Flv,      "23170f69-40c1-278a-1000-000110D60000".ParseAsGuid()},
            {SevenZipFormat.SquashFS, "23170f69-40c1-278a-1000-000110D20000".ParseAsGuid()},
            {SevenZipFormat.Lzma86,   "23170f69-40c1-278a-1000-0001100B0000".ParseAsGuid()},
            {SevenZipFormat.Ppmd,     "23170f69-40c1-278a-1000-0001100D0000".ParseAsGuid()},
            {SevenZipFormat.TE,       "23170f69-40c1-278a-1000-000110CF0000".ParseAsGuid()},
            {SevenZipFormat.UEFIc,    "23170f69-40c1-278a-1000-000110D00000".ParseAsGuid()},
            {SevenZipFormat.UEFIs,    "23170f69-40c1-278a-1000-000110D10000".ParseAsGuid()},
            {SevenZipFormat.CramFS,   "23170f69-40c1-278a-1000-000110D30000".ParseAsGuid()},
            {SevenZipFormat.APM,      "23170f69-40c1-278a-1000-000110D40000".ParseAsGuid()},
            {SevenZipFormat.Swfc,     "23170f69-40c1-278a-1000-000110D80000".ParseAsGuid()},
            {SevenZipFormat.Ntfs,     "23170f69-40c1-278a-1000-000110D90000".ParseAsGuid()},
            {SevenZipFormat.Fat,      "23170f69-40c1-278a-1000-000110DA0000".ParseAsGuid()},
            {SevenZipFormat.Mbr,      "23170f69-40c1-278a-1000-000110DB0000".ParseAsGuid()},
            {SevenZipFormat.MachO,    "23170f69-40c1-278a-1000-000110DF0000".ParseAsGuid()}
        }.ToFrozenDictionary();

        internal static FrozenDictionary<SevenZipFormat, FormatProperties> Signatures = new Dictionary<SevenZipFormat, FormatProperties>
        {
            {SevenZipFormat.Rar5,     FormatProperties.Create([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00])},
            {SevenZipFormat.Rar,      FormatProperties.Create([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00])},
            {SevenZipFormat.Vhd,      FormatProperties.Create("conectix"u8.ToArray())},
            {SevenZipFormat.Deb,      FormatProperties.Create("!<arch>"u8.ToArray())},
            {SevenZipFormat.Dmg,      FormatProperties.Create([0x78, 0x01, 0x73, 0x0D, 0x62, 0x62, 0x60])},
            {SevenZipFormat.SevenZip, FormatProperties.Create([0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C])},
            {SevenZipFormat.Tar,      FormatProperties.Create("ustar"u8.ToArray(),[0x101])},
            {SevenZipFormat.Iso,      FormatProperties.Create("CD001"u8.ToArray(),[0x8001, 0x8801, 0x9001])},
            {SevenZipFormat.Cab,      FormatProperties.Create("MSCF"u8.ToArray())},
            {SevenZipFormat.Rpm,      FormatProperties.Create([0xed, 0xab, 0xee, 0xdb])},
            {SevenZipFormat.Xar,      FormatProperties.Create("xar!"u8.ToArray())},
            {SevenZipFormat.Chm,      FormatProperties.Create("ITSF"u8.ToArray())},
            {SevenZipFormat.BZip2,    FormatProperties.Create("BZh"u8.ToArray())},
            {SevenZipFormat.Flv,      FormatProperties.Create("FLV"u8.ToArray())},
            {SevenZipFormat.Swf,      FormatProperties.Create("FWS"u8.ToArray())},
            {SevenZipFormat.GZip,     FormatProperties.Create([0x1f, 0x8b])},
            {SevenZipFormat.Zip,      FormatProperties.Create("PK"u8.ToArray())},
            {SevenZipFormat.Arj,      FormatProperties.Create([0x60, 0xEA])},
            {SevenZipFormat.Lzh,      FormatProperties.Create("-lh"u8.ToArray(),[0x2])},
            {SevenZipFormat.SquashFS, FormatProperties.Create("hsqs"u8.ToArray())},
            {SevenZipFormat.Mslz,     FormatProperties.Create("SZDD"u8.ToArray())}
        }.ToFrozenDictionary();
    }
}
