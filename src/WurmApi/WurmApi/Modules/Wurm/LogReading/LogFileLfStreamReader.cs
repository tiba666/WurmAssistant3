using System;
using System.Runtime.CompilerServices;

namespace AldursLab.WurmApi.Modules.Wurm.LogReading
{
    class LogFileLfStreamReader : LogFileStreamReader
    {
        public LogFileLfStreamReader(string fileFullPath, long startPosition = 0, bool trackFileBytePositions = false)
            : base(fileFullPath, startPosition, trackFileBytePositions)
        {
        }

        protected override string ReadCharsForNextLine()
        {
            while (true)
            {
                CurrentResult = StreamReader.Read();
                if (CurrentResult == -1)
                {
                    return StringBuilder.ToString();
                }
                CurrentChar = (char) CurrentResult;
                if (CurrentChar == '\n')
                {
                    return StringBuilder.ToString();
                }
                else
                {
                    StringBuilder.Append(CurrentChar);
                }
            }
        }

        public override void FastForwardLinesCount(int lineCountToSkip)
        {
            if (lineCountToSkip <= 0) return;

            int lineIndex = 0;
            int targetLineIndex = lineCountToSkip;
            while (true)
            {
                var result = StreamReader.Read();
                CheckEndOfFile(result, lineCountToSkip, lineIndex);
                if ((char)result == '\n')
                {
                    lineIndex++;
                    if (lineIndex == targetLineIndex)
                    {
                        return;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckEndOfFile(int result, int lineCountToSkip, int lineIndex)
        {
            if (result == -1)
                throw new InvalidOperationException(
                    string.Format("No more lines in the file! Last index: {0}, Intended to skip: {1}",
                        lineIndex,
                        lineCountToSkip));
        }
    }
}