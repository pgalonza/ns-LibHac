using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Crypto;
using LibHac.Fs;
using LibHac.Gc.Impl;
using LibHac.Tools.Crypto;
using LibHac.Tools.FsSystem;
using LibHac.Util;
using Aes = LibHac.Crypto.Aes;

namespace LibHac.Tools.Fs;

public class XciHeader
{
    private const int SignatureSize = 0x100;
    private const string HeaderMagic = "HEAD";
    private const uint HeaderMagicValue = 0x44414548; // HEAD
    private const int EncryptedHeaderSize = 0x70;
    private const int GcTitleKeyKekIndexMax = 0x10;

    private static readonly byte[] XciHeaderPubk =
    [
        0x98, 0xC7, 0x26, 0xB6, 0x0D, 0x0A, 0x50, 0xA7, 0x39, 0x21, 0x0A, 0xE3, 0x2F, 0xE4, 0x3E, 0x2E,
        0x5B, 0xA2, 0x86, 0x75, 0xAA, 0x5C, 0xEE, 0x34, 0xF1, 0xA3, 0x3A, 0x7E, 0xBD, 0x90, 0x4E, 0xF7,
        0x8D, 0xFA, 0x17, 0xAA, 0x6B, 0xC6, 0x36, 0x6D, 0x4C, 0x9A, 0x6D, 0x57, 0x2F, 0x80, 0xA2, 0xBC,
        0x38, 0x4D, 0xDA, 0x99, 0xA1, 0xD8, 0xC3, 0xE2, 0x99, 0x79, 0x36, 0x71, 0x90, 0x20, 0x25, 0x9D,
        0x4D, 0x11, 0xB8, 0x2E, 0x63, 0x6B, 0x5A, 0xFA, 0x1E, 0x9C, 0x04, 0xD1, 0xC5, 0xF0, 0x9C, 0xB1,
        0x0F, 0xB8, 0xC1, 0x7B, 0xBF, 0xE8, 0xB0, 0xD2, 0x2B, 0x47, 0x01, 0x22, 0x6B, 0x23, 0xC9, 0xD0,
        0xBC, 0xEB, 0x75, 0x6E, 0x41, 0x7D, 0x4C, 0x26, 0xA4, 0x73, 0x21, 0xB4, 0xF0, 0x14, 0xE5, 0xD9,
        0x8D, 0xB3, 0x64, 0xEE, 0xA8, 0xFA, 0x84, 0x1B, 0xB8, 0xB8, 0x7C, 0x88, 0x6B, 0xEF, 0xCC, 0x97,
        0x04, 0x04, 0x9A, 0x67, 0x2F, 0xDF, 0xEC, 0x0D, 0xB2, 0x5F, 0xB5, 0xB2, 0xBD, 0xB5, 0x4B, 0xDE,
        0x0E, 0x88, 0xA3, 0xBA, 0xD1, 0xB4, 0xE0, 0x91, 0x81, 0xA7, 0x84, 0xEB, 0x77, 0x85, 0x8B, 0xEF,
        0xA5, 0xE3, 0x27, 0xB2, 0xF2, 0x82, 0x2B, 0x29, 0xF1, 0x75, 0x2D, 0xCE, 0xCC, 0xAE, 0x9B, 0x8D,
        0xED, 0x5C, 0xF1, 0x8E, 0xDB, 0x9A, 0xD7, 0xAF, 0x42, 0x14, 0x52, 0xCD, 0xE3, 0xC5, 0xDD, 0xCE,
        0x08, 0x12, 0x17, 0xD0, 0x7F, 0x1A, 0xAA, 0x1F, 0x7D, 0xE0, 0x93, 0x54, 0xC8, 0xBC, 0x73, 0x8A,
        0xCB, 0xAD, 0x6E, 0x93, 0xE2, 0x19, 0x72, 0x6B, 0xD3, 0x45, 0xF8, 0x73, 0x3D, 0x2B, 0x6A, 0x55,
        0xD2, 0x3A, 0x8B, 0xB0, 0x8A, 0x42, 0xE3, 0x3D, 0xF1, 0x92, 0x23, 0x42, 0x2E, 0xBA, 0xCC, 0x9C,
        0x9A, 0xC1, 0xDD, 0x62, 0x86, 0x9C, 0x2E, 0xE1, 0x2D, 0x6F, 0x62, 0x67, 0x51, 0x08, 0x0E, 0xCF
    ];

    public byte[] Signature { get; set; }
    public string Magic { get; set; }
    public int RomAreaStartPage { get; set; }
    public int BackupAreaStartPage { get; set; }
    public byte KekIndex { get; set; }
    public byte TitleKeyDecIndex { get; set; }
    public GameCardSizeInternal GameCardSize { get; set; }
    public byte CardHeaderVersion { get; set; }
    public GameCardAttribute Flags { get; set; }
    public ulong PackageId { get; set; }
    public long ValidDataEndPage { get; set; }
    public byte[] AesCbcIv { get; set; }
    public long RootPartitionOffset { get; set; }
    public long RootPartitionHeaderSize { get; set; }
    public byte[] RootPartitionHeaderHash { get; set; }
    public byte[] InitialDataHash { get; set; }
    public int SelSec { get; set; }
    public int SelT1Key { get; set; }
    public int SelKey { get; set; }
    public int LimAreaPage { get; set; }

    public bool IsHeaderDecrypted { get; set; }
    public ulong FwVersion { get; set; }
    public CardClockRate AccCtrl1 { get; set; }
    public int Wait1TimeRead { get; set; }
    public int Wait2TimeRead { get; set; }
    public int Wait1TimeWrite { get; set; }
    public int Wait2TimeWrite { get; set; }
    public int FwMode { get; set; }
    public int UppVersion { get; set; }
    public byte CompatibilityType { get; set; }
    public byte[] UppHash { get; set; }
    public ulong UppId { get; set; }

    public byte[] ImageHash { get; }

    public Validity SignatureValidity { get; set; }
    public Validity PartitionFsHeaderValidity { get; set; }
    public Validity InitialDataValidity { get; set; }

    public bool HasInitialData { get; set; }
    public byte[] InitialDataPackageId { get; set; }
    public byte[] InitialDataAuthData { get; set; }
    public byte[] InitialDataAuthMac { get; set; }
    public byte[] InitialDataAuthNonce { get; set; }
    public byte[] InitialData { get; set; }
    public byte[] DecryptedTitleKey { get; set; }

    public XciHeader(KeySet keySet, Stream stream)
    {
        DetermineXciSubStorages(out IStorage keyAreaStorage, out IStorage bodyStorage, stream.AsStorage())
            .ThrowIfFailure();

        if (keyAreaStorage is not null)
        {
            using (var r = new BinaryReader(keyAreaStorage.AsStream(), Encoding.Default, true))
            {
                HasInitialData = true;
                InitialDataPackageId = r.ReadBytes(8);
                r.BaseStream.Position += 8;
                InitialDataAuthData = r.ReadBytes(0x10);
                InitialDataAuthMac = r.ReadBytes(0x10);
                InitialDataAuthNonce = r.ReadBytes(0xC);

                r.BaseStream.Position = 0;
                InitialData = r.ReadBytes(Unsafe.SizeOf<CardInitialData>());
            }
        }

        using (var reader = new BinaryReader(bodyStorage.AsStream(), Encoding.Default, true))
        {
            Signature = reader.ReadBytes(SignatureSize);
            Magic = reader.ReadAscii(4);
            if (Magic != HeaderMagic)
            {
                throw new InvalidDataException("Invalid XCI file: Header magic invalid.");
            }

            reader.BaseStream.Position = SignatureSize;
            byte[] sigData = reader.ReadBytes(SignatureSize);
            reader.BaseStream.Position = SignatureSize + 4;

            SignatureValidity = CryptoOld.Rsa2048Pkcs1Verify(sigData, Signature, XciHeaderPubk);

            RomAreaStartPage = reader.ReadInt32();
            BackupAreaStartPage = reader.ReadInt32();
            byte keyIndex = reader.ReadByte();
            KekIndex = (byte)(keyIndex >> 4);
            TitleKeyDecIndex = (byte)(keyIndex & 7);
            GameCardSize = (GameCardSizeInternal)reader.ReadByte();
            CardHeaderVersion = reader.ReadByte();
            Flags = (GameCardAttribute)reader.ReadByte();
            PackageId = reader.ReadUInt64();
            ValidDataEndPage = reader.ReadInt64();
            AesCbcIv = reader.ReadBytes(Aes.KeySize128);
            Array.Reverse(AesCbcIv);
            RootPartitionOffset = reader.ReadInt64();
            RootPartitionHeaderSize = reader.ReadInt64();
            RootPartitionHeaderHash = reader.ReadBytes(Sha256.DigestSize);
            InitialDataHash = reader.ReadBytes(Sha256.DigestSize);
            SelSec = reader.ReadInt32();
            SelT1Key = reader.ReadInt32();
            SelKey = reader.ReadInt32();
            LimAreaPage = reader.ReadInt32();

            if (keySet != null && !keySet.XciHeaderKey.IsZeros())
            {
                IsHeaderDecrypted = true;

                byte[] encHeader = reader.ReadBytes(EncryptedHeaderSize);
                byte[] decHeader = new byte[EncryptedHeaderSize];
                Aes.DecryptCbc128(encHeader, decHeader, keySet.XciHeaderKey, AesCbcIv);

                using (var decReader = new BinaryReader(new MemoryStream(decHeader)))
                {
                    FwVersion = decReader.ReadUInt64();
                    AccCtrl1 = (CardClockRate)decReader.ReadInt32();
                    Wait1TimeRead = decReader.ReadInt32();
                    Wait2TimeRead = decReader.ReadInt32();
                    Wait1TimeWrite = decReader.ReadInt32();
                    Wait2TimeWrite = decReader.ReadInt32();
                    FwMode = decReader.ReadInt32();
                    UppVersion = decReader.ReadInt32();
                    CompatibilityType = decReader.ReadByte();
                    decReader.BaseStream.Position += 3;
                    UppHash = decReader.ReadBytes(8);
                    UppId = decReader.ReadUInt64();
                }
            }

            ImageHash = new byte[Sha256.DigestSize];
            Sha256.GenerateSha256Hash(sigData, ImageHash);

            reader.BaseStream.Position = RootPartitionOffset;
            byte[] headerBytes = reader.ReadBytes((int)RootPartitionHeaderSize);
            Span<byte> actualHeaderHash = stackalloc byte[Sha256.DigestSize];

            Optional<byte> salt = CompatibilityType == 0 ? new Optional<byte>() : CompatibilityType;
            
            var generator = new Sha256Generator();
            generator.Initialize();
            generator.Update(headerBytes);
            if (salt.HasValue)
            {
                generator.Update(SpanHelpers.AsReadOnlyByteSpan(in salt.ValueRo));
            }

            generator.GetHash(actualHeaderHash);

            PartitionFsHeaderValidity = Utilities.SpansEqual(RootPartitionHeaderHash, actualHeaderHash) ? Validity.Valid : Validity.Invalid;

            if (HasInitialData)
            {
                Span<byte> actualInitialDataHash = stackalloc byte[Sha256.DigestSize];
                Sha256.GenerateSha256Hash(InitialData, actualInitialDataHash);

                InitialDataValidity = Utilities.SpansEqual(InitialDataHash, actualInitialDataHash)
                    ? Validity.Valid
                    : Validity.Invalid;
            }

            Span<byte> key = stackalloc byte[0x10];
            Result res = DecryptCardInitialData(key, InitialData, KekIndex, keySet);
            if (res.IsSuccess())
            {
                DecryptedTitleKey = key.ToArray();
            }
        }
    }

    private Result DecryptCardInitialData(Span<byte> dest, ReadOnlySpan<byte> initialData, int kekIndex, KeySet keySet)
    {
        if (initialData.Length != Unsafe.SizeOf<CardInitialData>())
            return ResultFs.GameCardPreconditionViolation.Log();

        if (kekIndex >= GcTitleKeyKekIndexMax)
            return ResultFs.GameCardPreconditionViolation.Log();

        // Verify the kek is preset.
        if (keySet.GcTitleKeyKeks[kekIndex].IsZeros())
            return ResultFs.GameCardPreconditionViolation.Log();

        // Generate the key.
        Span<byte> key = stackalloc byte[0x10];
        Aes.DecryptEcb128(initialData.Slice(0, 0x10), key, keySet.GcTitleKeyKeks[kekIndex]);

        ref readonly CardInitialData data = ref SpanHelpers.AsReadOnlyStruct<CardInitialData>(initialData);

        if (dest.Length != data.Payload.AuthData[..].Length)
            return ResultFs.GameCardPreconditionViolation.Log();

        // Verify padding is all-zero.
        bool anyNonZero = false;
        for (int i = 0; i < data.Padding.Length; i++)
        {
            anyNonZero |= data.Padding[i] != 0;
        }

        if (anyNonZero)
            return ResultFs.GameCardInitialNotFilledWithZero.Log();

        using (var decryptor = new AesCcm(key))
        {
            try
            {
                decryptor.Decrypt(data.Payload.AuthNonce, data.Payload.AuthData, data.Payload.AuthMac, dest);
            }
            catch (CryptographicException)
            {
                return ResultFs.GameCardKekIndexMismatch.Log();
            }
        }

        return Result.Success;
    }

    private Result DetermineXciSubStorages(out IStorage keyAreaStorage, out IStorage bodyStorage, IStorage baseStorage)
    {
        UnsafeHelpers.SkipParamInit(out keyAreaStorage, out bodyStorage);

        Result res = baseStorage.GetSize(out long storageSize);
        if (res.IsFailure()) return res.Miss();

        if (storageSize >= 0x1104)
        {
            uint magic = 0;
            res = baseStorage.Read(0x1100, SpanHelpers.AsByteSpan(ref magic));
            if (res.IsFailure()) return res.Miss();

            if (magic == HeaderMagicValue)
            {
                keyAreaStorage = new SubStorage(baseStorage, 0, 0x1000);
                bodyStorage = new SubStorage(baseStorage, 0x1000, storageSize - 0x1000);
                return Result.Success;
            }
        }

        keyAreaStorage = null;
        bodyStorage = baseStorage;
        return Result.Success;
    }
}

public enum CardClockRate
{
    ClockRate25 = 0xA10011,
    ClockRate50 = 0xA10010
}

public enum XciPartitionType
{
    Update,
    Normal,
    Secure,
    Logo,
    Root
}

public static class XciExtensions
{
    public static string GetFileName(this XciPartitionType type)
    {
        switch (type)
        {
            case XciPartitionType.Update: return "update";
            case XciPartitionType.Normal: return "normal";
            case XciPartitionType.Secure: return "secure";
            case XciPartitionType.Logo: return "logo";
            case XciPartitionType.Root: return "root";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}