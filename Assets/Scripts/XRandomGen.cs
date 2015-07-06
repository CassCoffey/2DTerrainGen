using UnityEngine;
using System.Collections;
using System;

public class XRandomGen {

    int seed;

    public XRandomGen(int seed)
    {
        this.seed = seed;
    }

    public int GetInt(float x)
    {
        int w =  (int)(Mathf.Cos(seed * x) * int.MaxValue);
        int f = w & unchecked((int)0xffff0000);
        int b = w & unchecked((int)0x0000ffff);
        b = b << 16;
        w = b + f;
        w ^= 1;
        return w;
    }

    public float GetRange(float x, float min, float max)
    {
        int w = GetInt(x);

        float range = max - min;
        float normalized = (float)(w + int.MaxValue) / uint.MaxValue;
        normalized += 0.5f;
        float withinRange = (normalized * (float)range) + (float)min;
        return withinRange;
    }

    public int GetRange(float x, int min, int max)
    {
        return (int)GetRange(x, (float)min, (float)max);
    }
}
