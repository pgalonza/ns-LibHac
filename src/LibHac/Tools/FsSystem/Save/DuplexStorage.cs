﻿using System;
using LibHac.Fs;

namespace LibHac.Tools.FsSystem.Save;

public class DuplexStorage : IStorage
{
    private int BlockSize { get; }
    private IStorage BitmapStorage { get; }
    private IStorage DataA { get; }
    private IStorage DataB { get; }
    private DuplexBitmap Bitmap { get; }

    private long Length { get; }

    public DuplexStorage(IStorage dataA, IStorage dataB, IStorage bitmap, int blockSize)
    {
        DataA = dataA;
        DataB = dataB;
        BitmapStorage = bitmap;
        BlockSize = blockSize;

        bitmap.GetSize(out long bitmapSize).ThrowIfFailure();

        Bitmap = new DuplexBitmap(BitmapStorage, (int)(bitmapSize * 8));
        DataA.GetSize(out long dataSize).ThrowIfFailure();
        Length = dataSize;
    }

    public override Result Read(long offset, Span<byte> destination)
    {
        long inPos = offset;
        int outPos = 0;
        int remaining = destination.Length;

        if (!CheckAccessRange(offset, destination.Length, Length))
            return ResultFs.OutOfRange.Log();

        while (remaining > 0)
        {
            int blockNum = (int)(inPos / BlockSize);
            int blockPos = (int)(inPos % BlockSize);

            int bytesToRead = Math.Min(remaining, BlockSize - blockPos);

            IStorage data = Bitmap.Bitmap[blockNum] ? DataB : DataA;

            Result rc = data.Read(inPos, destination.Slice(outPos, bytesToRead));
            if (rc.IsFailure()) return rc;

            outPos += bytesToRead;
            inPos += bytesToRead;
            remaining -= bytesToRead;
        }

        return Result.Success;
    }

    public override Result Write(long offset, ReadOnlySpan<byte> source)
    {
        long inPos = offset;
        int outPos = 0;
        int remaining = source.Length;

        if (!CheckAccessRange(offset, source.Length, Length))
            return ResultFs.OutOfRange.Log();

        while (remaining > 0)
        {
            int blockNum = (int)(inPos / BlockSize);
            int blockPos = (int)(inPos % BlockSize);

            int bytesToWrite = Math.Min(remaining, BlockSize - blockPos);

            IStorage data = Bitmap.Bitmap[blockNum] ? DataB : DataA;

            Result rc = data.Write(inPos, source.Slice(outPos, bytesToWrite));
            if (rc.IsFailure()) return rc;

            outPos += bytesToWrite;
            inPos += bytesToWrite;
            remaining -= bytesToWrite;
        }

        return Result.Success;
    }

    public override Result Flush()
    {
        Result rc = BitmapStorage.Flush();
        if (rc.IsFailure()) return rc;

        rc = DataA.Flush();
        if (rc.IsFailure()) return rc;

        rc = DataB.Flush();
        if (rc.IsFailure()) return rc;

        return Result.Success;
    }

    public override Result SetSize(long size)
    {
        return ResultFs.NotImplemented.Log();
    }

    public override Result GetSize(out long size)
    {
        size = Length;
        return Result.Success;
    }

    public override Result OperateRange(Span<byte> outBuffer, OperationId operationId, long offset, long size,
        ReadOnlySpan<byte> inBuffer)
    {
        throw new NotImplementedException();
    }

    public void FsTrim()
    {
        DataA.GetSize(out long dataSize).ThrowIfFailure();

        int blockCount = (int)(dataSize / BlockSize);

        for (int i = 0; i < blockCount; i++)
        {
            IStorage dataToClear = Bitmap.Bitmap[i] ? DataA : DataB;

            dataToClear.Slice(i * BlockSize, BlockSize).Fill(SaveDataFileSystem.TrimFillValue);
        }
    }
}