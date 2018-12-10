﻿using System.Linq;
using LibHac.IO;
using Xunit;

namespace LibHac.Tests
{
    public class AesXts
    {
        private static readonly TestData[] TestVectors =
        {
            // #1 32 byte key, 32 byte PTX 
            new TestData
            {
                Key1 = new byte[]
                    {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                Key2 = new byte[]
                    {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                Sector = 0,
                PlainText = new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                },
                CipherText = new byte[]
                {
                    0x91, 0x7C, 0xF6, 0x9E, 0xBD, 0x68, 0xB2, 0xEC, 0x9B, 0x9F, 0xE9, 0xA3, 0xEA, 0xDD, 0xA6, 0x92,
                    0xCD, 0x43, 0xD2, 0xF5, 0x95, 0x98, 0xED, 0x85, 0x8C, 0x02, 0xC2, 0x65, 0x2F, 0xBF, 0x92, 0x2E
                }
            },

            // #2, 32 byte key, 32 byte PTX 
            new TestData
            {
                Key1 = new byte[]
                    {0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11},
                Key2 = new byte[]
                    {0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22},
                Sector = 0x3333333333,
                PlainText = new byte[]
                {
                    0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44,
                    0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44
                },
                CipherText = new byte[]
                {
                    0x44, 0xBE, 0xC8, 0x2F, 0xFB, 0x76, 0xAE, 0xFD, 0xFB, 0xC9, 0x6D, 0xFE, 0x61, 0xE1, 0x92, 0xCC,
                    0xFA, 0x22, 0x13, 0x67, 0x7C, 0x8F, 0x4F, 0xD6, 0xE4, 0xF1, 0x8F, 0x7E, 0xBB, 0x69, 0x38, 0x2F
                },
            },

            // #5 from xts.7, 32 byte key, 32 byte PTX 
            new TestData
            {
                Key1 = new byte[]
                    {0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8, 0xF7, 0xF6, 0xF5, 0xF4, 0xF3, 0xF2, 0xF1, 0xF0},
                Key2 = new byte[]
                    {0xBF, 0xBE, 0xBD, 0xBC, 0xBB, 0xBA, 0xB9, 0xB8, 0xB7, 0xB6, 0xB5, 0xB4, 0xB3, 0xB2, 0xB1, 0xB0},
                Sector = 0x123456789A,
                PlainText = new byte[]
                {
                    0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44,
                    0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44
                },
                CipherText = new byte[]
                {
                    0xC1, 0x18, 0x39, 0xD6, 0x36, 0xAD, 0x8B, 0xE5, 0xA1, 0x16, 0xE4, 0x8C, 0x70, 0x22, 0x77, 0x63,
                    0xDA, 0xBD, 0x3C, 0x2D, 0x13, 0x83, 0xC5, 0xDD, 0x15, 0xB2, 0x57, 0x2A, 0xAA, 0x99, 0x2C, 0x40,
                },
            },

            // #4, 32 byte key, 512 byte PTX  
            new TestData
            {
                Key1 = new byte[]
                    {0x27, 0x18, 0x28, 0x18, 0x28, 0x45, 0x90, 0x45, 0x23, 0x53, 0x60, 0x28, 0x74, 0x71, 0x35, 0x26},
                Key2 = new byte[]
                    {0x31, 0x41, 0x59, 0x26, 0x53, 0x58, 0x97, 0x93, 0x23, 0x84, 0x62, 0x64, 0x33, 0x83, 0x27, 0x95},
                Sector = 0,
                PlainText = new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
                    0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
                    0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
                    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
                    0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
                    0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
                    0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
                    0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
                    0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
                    0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
                    0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
                    0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
                    0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
                    0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
                    0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
                    0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
                    0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
                    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
                    0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
                    0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
                    0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
                    0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
                    0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
                    0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
                    0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
                    0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
                    0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
                    0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
                    0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
                },
                CipherText = new byte[]
                {
                    0x27, 0xA7, 0x47, 0x9B, 0xEF, 0xA1, 0xD4, 0x76, 0x48, 0x9F, 0x30, 0x8C, 0xD4, 0xCF, 0xA6, 0xE2,
                    0xA9, 0x6E, 0x4B, 0xBE, 0x32, 0x08, 0xFF, 0x25, 0x28, 0x7D, 0xD3, 0x81, 0x96, 0x16, 0xE8, 0x9C,
                    0xC7, 0x8C, 0xF7, 0xF5, 0xE5, 0x43, 0x44, 0x5F, 0x83, 0x33, 0xD8, 0xFA, 0x7F, 0x56, 0x00, 0x00,
                    0x05, 0x27, 0x9F, 0xA5, 0xD8, 0xB5, 0xE4, 0xAD, 0x40, 0xE7, 0x36, 0xDD, 0xB4, 0xD3, 0x54, 0x12,
                    0x32, 0x80, 0x63, 0xFD, 0x2A, 0xAB, 0x53, 0xE5, 0xEA, 0x1E, 0x0A, 0x9F, 0x33, 0x25, 0x00, 0xA5,
                    0xDF, 0x94, 0x87, 0xD0, 0x7A, 0x5C, 0x92, 0xCC, 0x51, 0x2C, 0x88, 0x66, 0xC7, 0xE8, 0x60, 0xCE,
                    0x93, 0xFD, 0xF1, 0x66, 0xA2, 0x49, 0x12, 0xB4, 0x22, 0x97, 0x61, 0x46, 0xAE, 0x20, 0xCE, 0x84,
                    0x6B, 0xB7, 0xDC, 0x9B, 0xA9, 0x4A, 0x76, 0x7A, 0xAE, 0xF2, 0x0C, 0x0D, 0x61, 0xAD, 0x02, 0x65,
                    0x5E, 0xA9, 0x2D, 0xC4, 0xC4, 0xE4, 0x1A, 0x89, 0x52, 0xC6, 0x51, 0xD3, 0x31, 0x74, 0xBE, 0x51,
                    0xA1, 0x0C, 0x42, 0x11, 0x10, 0xE6, 0xD8, 0x15, 0x88, 0xED, 0xE8, 0x21, 0x03, 0xA2, 0x52, 0xD8,
                    0xA7, 0x50, 0xE8, 0x76, 0x8D, 0xEF, 0xFF, 0xED, 0x91, 0x22, 0x81, 0x0A, 0xAE, 0xB9, 0x9F, 0x91,
                    0x72, 0xAF, 0x82, 0xB6, 0x04, 0xDC, 0x4B, 0x8E, 0x51, 0xBC, 0xB0, 0x82, 0x35, 0xA6, 0xF4, 0x34,
                    0x13, 0x32, 0xE4, 0xCA, 0x60, 0x48, 0x2A, 0x4B, 0xA1, 0xA0, 0x3B, 0x3E, 0x65, 0x00, 0x8F, 0xC5,
                    0xDA, 0x76, 0xB7, 0x0B, 0xF1, 0x69, 0x0D, 0xB4, 0xEA, 0xE2, 0x9C, 0x5F, 0x1B, 0xAD, 0xD0, 0x3C,
                    0x5C, 0xCF, 0x2A, 0x55, 0xD7, 0x05, 0xDD, 0xCD, 0x86, 0xD4, 0x49, 0x51, 0x1C, 0xEB, 0x7E, 0xC3,
                    0x0B, 0xF1, 0x2B, 0x1F, 0xA3, 0x5B, 0x91, 0x3F, 0x9F, 0x74, 0x7A, 0x8A, 0xFD, 0x1B, 0x13, 0x0E,
                    0x94, 0xBF, 0xF9, 0x4E, 0xFF, 0xD0, 0x1A, 0x91, 0x73, 0x5C, 0xA1, 0x72, 0x6A, 0xCD, 0x0B, 0x19,
                    0x7C, 0x4E, 0x5B, 0x03, 0x39, 0x36, 0x97, 0xE1, 0x26, 0x82, 0x6F, 0xB6, 0xBB, 0xDE, 0x8E, 0xCC,
                    0x1E, 0x08, 0x29, 0x85, 0x16, 0xE2, 0xC9, 0xED, 0x03, 0xFF, 0x3C, 0x1B, 0x78, 0x60, 0xF6, 0xDE,
                    0x76, 0xD4, 0xCE, 0xCD, 0x94, 0xC8, 0x11, 0x98, 0x55, 0xEF, 0x52, 0x97, 0xCA, 0x67, 0xE9, 0xF3,
                    0xE7, 0xFF, 0x72, 0xB1, 0xE9, 0x97, 0x85, 0xCA, 0x0A, 0x7E, 0x77, 0x20, 0xC5, 0xB3, 0x6D, 0xC6,
                    0xD7, 0x2C, 0xAC, 0x95, 0x74, 0xC8, 0xCB, 0xBC, 0x2F, 0x80, 0x1E, 0x23, 0xE5, 0x6F, 0xD3, 0x44,
                    0xB0, 0x7F, 0x22, 0x15, 0x4B, 0xEB, 0xA0, 0xF0, 0x8C, 0xE8, 0x89, 0x1E, 0x64, 0x3E, 0xD9, 0x95,
                    0xC9, 0x4D, 0x9A, 0x69, 0xC9, 0xF1, 0xB5, 0xF4, 0x99, 0x02, 0x7A, 0x78, 0x57, 0x2A, 0xEE, 0xBD,
                    0x74, 0xD2, 0x0C, 0xC3, 0x98, 0x81, 0xC2, 0x13, 0xEE, 0x77, 0x0B, 0x10, 0x10, 0xE4, 0xBE, 0xA7,
                    0x18, 0x84, 0x69, 0x77, 0xAE, 0x11, 0x9F, 0x7A, 0x02, 0x3A, 0xB5, 0x8C, 0xCA, 0x0A, 0xD7, 0x52,
                    0xAF, 0xE6, 0x56, 0xBB, 0x3C, 0x17, 0x25, 0x6A, 0x9F, 0x6E, 0x9B, 0xF1, 0x9F, 0xDD, 0x5A, 0x38,
                    0xFC, 0x82, 0xBB, 0xE8, 0x72, 0xC5, 0x53, 0x9E, 0xDB, 0x60, 0x9E, 0xF4, 0xF7, 0x9C, 0x20, 0x3E,
                    0xBB, 0x14, 0x0F, 0x2E, 0x58, 0x3C, 0xB2, 0xAD, 0x15, 0xB4, 0xAA, 0x5B, 0x65, 0x50, 0x16, 0xA8,
                    0x44, 0x92, 0x77, 0xDB, 0xD4, 0x77, 0xEF, 0x2C, 0x8D, 0x6C, 0x01, 0x7D, 0xB7, 0x38, 0xB1, 0x8D,
                    0xEB, 0x4A, 0x42, 0x7D, 0x19, 0x23, 0xCE, 0x3F, 0xF2, 0x62, 0x73, 0x57, 0x79, 0xA4, 0x18, 0xF2,
                    0x0A, 0x28, 0x2D, 0xF9, 0x20, 0x14, 0x7B, 0xEA, 0xBE, 0x42, 0x1E, 0xE5, 0x31, 0x9D, 0x05, 0x68
                },
            },

            // #7, 32 byte key, 17 byte PTX 
            new TestData
            {
                Key1 = new byte[]
                    {0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8, 0xF7, 0xF6, 0xF5, 0xF4, 0xF3, 0xF2, 0xF1, 0xF0},
                Key2 = new byte[]
                    {0xBF, 0xBE, 0xBD, 0xBC, 0xBB, 0xBA, 0xB9, 0xB8, 0xB7, 0xB6, 0xB5, 0xB4, 0xB3, 0xB2, 0xB1, 0xB0},
                Sector = 0x123456789A,
                PlainText = new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
                },
                CipherText = new byte[]
                {
                    0x9E, 0x61, 0x71, 0x58, 0x09, 0xA7, 0x4B, 0x7E, 0x0E, 0xF0, 0x33, 0xCD, 0x86, 0x18, 0x14, 0x04, 0xC2
                },
            },

            // #15, 32 byte key, 25 byte PTX 
            new TestData
            {
                Key1 = new byte[]
                    {0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8, 0xF7, 0xF6, 0xF5, 0xF4, 0xF3, 0xF2, 0xF1, 0xF0},
                Key2 = new byte[]
                    {0xBF, 0xBE, 0xBD, 0xBC, 0xBB, 0xBA, 0xB9, 0xB8, 0xB7, 0xB6, 0xB5, 0xB4, 0xB3, 0xB2, 0xB1, 0xB0},
                Sector = 0x123456789A,
                PlainText = new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
                },
                CipherText = new byte[]
                {
                    0x5D, 0x0B, 0x4A, 0x86, 0xEC, 0x5A, 0x91, 0xFB, 0x84, 0x9D, 0x0F, 0x82, 0x6A, 0x31, 0x62, 0x22,
                    0xC2, 0x74, 0xAD, 0x93, 0xFC, 0x68, 0xC2, 0xC1, 0x01
                },
            },

            // #21, 32 byte key, 31 byte PTX 
            new TestData
            {
                Key1 = new byte[]
                    {0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8, 0xF7, 0xF6, 0xF5, 0xF4, 0xF3, 0xF2, 0xF1, 0xF0},
                Key2 = new byte[]
                    {0xBF, 0xBE, 0xBD, 0xBC, 0xBB, 0xBA, 0xB9, 0xB8, 0xB7, 0xB6, 0xB5, 0xB4, 0xB3, 0xB2, 0xB1, 0xB0},
                Sector = 0x123456789A,
                PlainText = new byte[]
                {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E
                },
                CipherText = new byte[]
                {
                    0x42, 0x67, 0x3C, 0x89, 0x7D, 0x4F, 0x53, 0x2C, 0xF8, 0xAA, 0x65, 0xEE, 0xB4, 0xD5, 0xB6, 0xF5,
                    0xC2, 0x74, 0xAD, 0x93, 0xFC, 0x68, 0xC2, 0xC1, 0x01, 0x5D, 0x90, 0x4F, 0x33, 0xFF, 0x95
                },
            }
        };

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public static void Encrypt(int index)
        {
            TestData data = TestVectors[index];
            var transform = new Aes128XtsTransform(data.Key1, data.Key2, false);
            byte[] encrypted = data.PlainText.ToArray();

            transform.TransformBlock(encrypted, 0, encrypted.Length, data.Sector);
            Assert.Equal(data.CipherText, encrypted);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public static void Decrypt(int index)
        {
            TestData data = TestVectors[index];
            byte[] decrypted = data.CipherText.ToArray();
            var transform = new Aes128XtsTransform(data.Key1, data.Key2, true);

            transform.TransformBlock(decrypted, 0, decrypted.Length, data.Sector);
            Assert.Equal(data.PlainText, decrypted);
        }

        private struct TestData
        {
            public byte[] CipherText;
            public byte[] PlainText;
            public byte[] Key1;
            public byte[] Key2;
            public ulong Sector;
        }
    }
}