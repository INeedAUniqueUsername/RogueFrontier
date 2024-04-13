using System;
using System.Collections.Generic;
using System.Numerics;

namespace Common;

//https://stackoverflow.com/a/46241672
public class Rand {
    private int _seed;
    private int _inext;
    private int _inextp;
    private int[] _seedArray = new int[56];

    /// <summary>
    /// The current seed of this instance.
    /// </summary>
    public int Seed {
        get {
            return _seed;
        }
        set {
            _seed = value;
            int subtraction = (_seed == Int32.MinValue) ? Int32.MaxValue : Math.Abs(_seed);
            int mj = 0x9a4ec86 - subtraction;
            _seedArray[0x37] = mj;
            int mk = 1;
            for (int i = 1; i < 0x37; i++) {
                int ii = (0x15 * i) % 0x37;
                _seedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0x0) {
                    mk += Int32.MaxValue;
                }
                mj = _seedArray[ii];
            }
            for (int k = 1; k < 0x5; k++) {
                for (int i = 1; i < 0x38; i++) {
                    _seedArray[i] -= _seedArray[1 + (i + 0x1e) % 0x37];
                    if (_seedArray[i] < 0) {
                        _seedArray[i] += Int32.MaxValue;
                    }
                }
            }
            _inext = 0;
            _inextp = 21;
        }
    }

    public Rand()
        // Used to generate a varying seed, regardless of close-frequency allocation
        : this(Guid.NewGuid().GetHashCode()) { }

    public Rand(string seed)
        : this(seed.GetHashCode()) { }

    public Rand(int[] saveState) {
        LoadState(saveState);
    }

    public Rand(int seed) {
        Seed = seed;
    }

    /// <summary>
    /// Resets this instance using it's current seed.
    /// This means that the RNG will start over again,
    /// repeating the same values that it originally had.
    /// </summary>
    public void Reset() {
        Reseed(this.Seed);
    }

    /// <summary>
    /// Reseeds this instance using a new GUID Hashcode
    /// </summary>
    public void Reseed() {
        Reseed(Guid.NewGuid().GetHashCode());
    }

    /// <summary>
    /// Reseeds this instance using the hashcode of a given string.
    /// </summary>
    /// <param name="seed"></param>
    public void Reseed(string seed) {
        Reseed(seed.GetHashCode());
    }

    /// <summary>
    /// Reseeds this instance using a given integer seed.
    /// </summary>
    /// <param name="seed"></param>
    public void Reseed(int seed) {
        this.Seed = seed;
    }

    public int NextInteger() {
        return NextSample();
    }

    public void NextIntegers(int[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextInteger();
        }
    }

    public int[] NextIntegers(int quantity) {
        int[] buffer = new int[quantity];
        NextIntegers(buffer);
        return buffer;
    }


    public int NextInteger(int minValue, int maxValue) {
        return (int)(this.NextSample() * (1.0D / Int32.MaxValue) * (maxValue - minValue)) + minValue;
    }


    public void NextIntegers(int minValue, int maxValue, int[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextInteger(minValue, maxValue);
        }
    }


    public int[] NextIntegers(int minValue, int maxValue, int quantity) {
        int[] buffer = new int[quantity];
        NextIntegers(minValue, maxValue, buffer);
        return buffer;
    }

    public int NextInteger(int maxValue) {
        return NextInteger(0, maxValue);
    }

    public void NextIntegers(int maxValue, int[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextInteger(maxValue);
        }
    }

    public int[] NextIntegers(int maxValue, int quantity) {
        int[] buffer = new int[quantity];
        NextIntegers(maxValue, buffer);
        return buffer;
    }

    public double NextDouble() {
        return NextSample() * (1.0D / Int32.MaxValue);
    }

    public void NextDoubles(double[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextDouble();
        }
    }

    public double[] NextDoubles(int quantity) {
        double[] buffer = new double[quantity];
        NextDoubles(buffer);
        return buffer;
    }

    public double NextDouble(double minValue, double maxValue) {
        return NextDouble() * (maxValue - minValue) + minValue;
    }

    public void NextDoubles(double minValue, double maxValue, double[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextDouble(minValue, maxValue);
        }
    }

    public double[] NextDoubles(double minValue, double maxValue, int quantity) {
        double[] buffer = new double[quantity];
        NextDoubles(minValue, maxValue, buffer);
        return buffer;
    }

    public float NextFloat() {
        return (float)(NextSample() * (1.0D / Int32.MaxValue));
    }

    public void NextFloats(float[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextFloat();
        }
    }

    public float[] NextFloats(int quantity) {
        float[] buffer = new float[quantity];
        NextFloats(buffer);
        return buffer;
    }

    public float NextFloat(float minValue, float maxValue) {
        return NextFloat() * (maxValue - minValue) + minValue;
    }

    public void NextFloats(float minValue, float maxValue, float[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextFloat(minValue, maxValue);
        }
    }

    public float[] NextFloats(float minValue, float maxValue, int quantity) {
        float[] buffer = new float[quantity];
        NextFloats(minValue, maxValue, buffer);
        return buffer;
    }

    public int NextRange(Range range) {
        return NextInteger(range.Start.Value, 1 + range.End.Value);
    }

    public void NextRanges(Range range, int[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextRange(range);
        }
    }

    public int[] NextRanges(Range range, int quantity) {
        int[] buffer = new int[quantity];
        NextRanges(range, buffer);
        return buffer;
    }

    public int NextRange(int minValue, int maxValue) {
        return NextInteger(minValue, maxValue + 1);
    }

    public void NextRanges(int minValue, int maxValue, int[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextRange(minValue, maxValue);
        }
    }

    public int[] NextRanges(int minValue, int maxValue, int quantity) {
        int[] buffer = new int[quantity];
        NextRanges(minValue, maxValue, buffer);
        return buffer;
    }

    public byte NextByte() {
        return (byte)NextInteger(0, 256);
    }

    public void NextBytes(byte[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextByte();
        }
    }

    public byte[] NextBytes(int quantity) {
        byte[] buffer = new byte[quantity];
        NextBytes(buffer);
        return buffer;
    }

    public byte NextByte(byte minValue, byte maxValue) {
        return (byte)NextInteger(minValue, maxValue);
    }

    public void NextBytes(byte minValue, byte maxValue, byte[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextByte(minValue, maxValue);
        }
    }

    public byte[] NextBytes(byte minValue, byte maxValue, int quantity) {
        byte[] buffer = new byte[quantity];
        NextBytes(minValue, maxValue, buffer);
        return buffer;
    }

    public byte NextByte(byte maxValue) {
        return (byte)NextInteger(0, maxValue);
    }

    public void NextBytes(byte maxValue, byte[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextByte(maxValue);
        }
    }

    public byte[] NextBytes(byte maxValue, int quantity) {
        byte[] buffer = new byte[quantity];
        NextBytes(maxValue, buffer);
        return buffer;
    }

    public bool NextBool() {
        return NextInteger(0, 2) == 1;
    }

    public void NextBools(bool[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextBool();
        }
    }

    public bool[] NextBools(int quantity) {
        bool[] buffer = new bool[quantity];
        NextBools(buffer);
        return buffer;
    }

    public string NextString(char[] possibleCharacters, int length) {
        char[] buffer = new char[length];
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = possibleCharacters[NextInteger(0, possibleCharacters.Length)];
        }
        return new string(buffer);
    }

    public void NextStrings(char[] possibleCharacters, int length, string[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextString(possibleCharacters, length);
        }
    }

    public string[] NextStrings(char[] possibleCharacters, int length, int quantity) {
        string[] buffer = new string[quantity];
        NextStrings(possibleCharacters, length, buffer);
        return buffer;
    }

    public void Shuffle<T>(List<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = NextInteger(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void Shuffle<T>(T[] list) {
        int n = list.Length;
        while (n > 1) {
            n--;
            int k = NextInteger(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public int NextIndex<T>(T[] array) {
        return NextInteger(array.Length);
    }

    public int NextIndex<T>(List<T> list) {
        return NextInteger(list.Count);
    }

    public T Choose<T>(T[] items) {
        return items[NextInteger(items.Length)];
    }

    public void Choose<T>(T[] items, T[] resultBuffer) {
        for (int i = 0; i < resultBuffer.Length; i++) {
            resultBuffer[i] = Choose(items);
        }
    }

    public T[] Choose<T>(T[] items, int quantity) {
        T[] buffer = new T[quantity];
        Choose(items, buffer);
        return buffer;
    }

    public T Choose<T>(List<T> items) {
        return items[NextInteger(0, items.Count)];
    }

    public List<T> Choose<T>(List<T> items, int quantity) {
        List<T> buffer = new(quantity);
        for (int i = 0; i < quantity; i++) {
            buffer.Add(Choose(items));
        }
        return buffer;
    }

    public bool NextProbability(float percent) {
        if (percent >= 1.0f) return true;
        if (percent <= 0.0f) return false;
        return NextDouble() <= percent;
    }

    public void NextProbability(float percent, bool[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextProbability(percent);
        }
    }

    public bool[] NextProbabilities(float percent, int quantity) {
        bool[] buffer = new bool[quantity];
        NextProbability(percent, buffer);
        return buffer;
    }

    public bool NextProbability(int percent) {
        if (percent >= 100) return true;
        if (percent <= 0) return false;
        return NextInteger(101) <= percent;
    }

    public void NextProbabilities(int percent, bool[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextProbability(percent);
        }
    }

    public bool[] NextProbabilities(int percent, int quantity) {
        bool[] buffer = new bool[quantity];
        NextProbabilities(percent, buffer);
        return buffer;
    }

    public bool NextOdds(int a, int b) {
        return NextProbability(((float)a / b));
    }

    public void NextOdds(int a, int b, bool[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextOdds(a, b);
        }
    }

    public bool[] NextOdds(int a, int b, int quantity) {
        bool[] buffer = new bool[quantity];
        NextOdds(a, b, buffer);
        return buffer;
    }
    public int NextDiceRoll(int d) {
        return NextInteger(1, d + 1);
    }

    public void NextDiceRolls(int d, int[] buffer) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = NextDiceRoll(d);
        }
    }

    public int[] NextDiceRolls(int d, int quantity) {
        int[] buffer = new int[quantity];
        NextDiceRolls(d, buffer);
        return buffer;
    }

    public Vector3 NextVector3(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax) {
        return new Vector3(NextFloat(xMin, xMax), NextFloat(yMin, yMax), NextFloat(zMin, zMax));
    }

    public Vector3 NextVector3(Vector3 min, Vector3 max) {
        return NextVector3(min.X, max.X, min.Y, max.Y, min.Z, max.Z);
    }

    public Vector3 NextVector3(float min, float max) {
        return NextVector3(min, max, min, max, min, max);
    }

    private int NextSample() {
        int locINext = _inext;
        int locINextp = _inextp;
        if (++locINext >= 56) {
            locINext = 1;
        }
        if (++locINextp >= 56) {
            locINextp = 1;
        }
        int retVal = _seedArray[locINext] - _seedArray[locINextp];
        if (retVal == Int32.MaxValue) {
            retVal--;
        }
        if (retVal < 0) {
            retVal += Int32.MaxValue;
        }
        _seedArray[locINext] = retVal;
        _inext = locINext;
        _inextp = locINextp;
        return retVal;
    }

    public int[] GetState() {
        int[] state = new int[59];
        state[0] = _seed;
        state[1] = _inext;
        state[2] = _inextp;
        for (int i = 3; i < this._seedArray.Length; i++) {
            state[i] = _seedArray[i - 3];
        }
        return state;
    }

    public void LoadState(int[] saveState) {
        if (saveState.Length != 59) {
            throw new Exception("GrimoireRandom state was corrupted!");
        }
        _seed = saveState[0];
        _inext = saveState[1];
        _inextp = saveState[2];
        _seedArray = new int[59];
        for (int i = 3; i < this._seedArray.Length; i++) {
            _seedArray[i - 3] = saveState[i];
        }
    }
}
