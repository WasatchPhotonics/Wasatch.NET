﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APITest
{
    public class Command
    {
        public enum Direction { HOST_TO_DEVICE, DEVICE_TO_HOST };
        public enum DataType
        {
            BOOL,
            BYTE_ARRAY,
            ENUM,
            FLOAT16,          // "FunkyFloat" with MSB integer and LSB fraction
            STRING,
            UINT8,
            UINT12,            // 12-bit DAC/ADC
            UINT16,
            UINT16_ARRAY,
            UINT32,
            UINT40,            // 5-byte values
        };

        public enum UsableFields { WVALUE, WINDEX };

        public string name;
        public byte opcode;
        public string units;
        public Direction direction = Direction.HOST_TO_DEVICE;
        public DataType dataType = DataType.BYTE_ARRAY;

        public int length = 0;
        public int readBack = 0;
        public int readBackARM = 0;
        public int readEndpoint = 0;
        public int readBlockSize = 0;
        public int fakeBufferLength = 0;
        public bool makeFakeBufferFromValue = false;

        public HashSet<UsableFields> usesFields;
        public HashSet<string> supportsBoards; // make HashSet of enum?
        public Tuple<double, double> range;
        public List<string> enumValues;

        public UInt16 wValue = 0;
        public UInt16 wIndex = 0;
        public bool fixedWValue = false;
        public bool fixedWIndex = false;

        public bool armInvertedReturn = false;
        public bool reverse = false;
        public bool enabled = true;

        public string notes;


    }
}