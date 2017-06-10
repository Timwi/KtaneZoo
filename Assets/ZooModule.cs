using System;
using System.Collections.Generic;
using System.Linq;
using Zoo;
using UnityEngine;
using System.Collections;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Zoos
/// Created by Timwi
/// </summary>
public class ZooModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        var portTypes = Enum.GetValues(typeof(PortType)).Cast<PortType>().ToArray();

        var portTypeToDir = new int[6];
        portTypeToDir[(int) PortType.Serial] = 0;
        portTypeToDir[(int) PortType.PS2] = 1;
        portTypeToDir[(int) PortType.RJ45] = 2;
        portTypeToDir[(int) PortType.Parallel] = 3;
        portTypeToDir[(int) PortType.DVI] = 4;
        portTypeToDir[(int) PortType.StereoRCA] = 5;

        // Wait a little to ensure that edgework is available.
        yield return new WaitForSeconds(.1f);

        var counts = Ut.NewArray(Bomb.GetPortPlateCount() + 1, j => j == 0 ? portTypes.ToHashSet() : new HashSet<PortType>());
        foreach (var port in Bomb.GetPorts())
        {
            var pIx = portTypes.IndexOf(pt => pt.ToString() == port);
            if (pIx != -1)
            {
                var pt = portTypes[pIx];
                var ix = counts.IndexOf(c => c.Contains(pt));
                counts[ix].Remove(pt);
                counts[ix + 1].Add(pt);
            }
        }

        var hexes = Hex.LargeHexagon(5).Select(hex =>
        {
            // Check the port types in order of most common to least common.
            for (int c = counts.Length - 1; c >= 0; c--)
            {
                // Check which of these port types can form a line of 5
                var eligiblePortTypes = counts[c].Where(pt => Enumerable.Range(0, 5).All(dist => (hex + dist * Hex.GetDirection(portTypeToDir[(int) pt])).Distance < 5)).Take(2).ToArray();
                if (eligiblePortTypes.Length != 1)
                    continue;

                // We found an eligible port type; return the line
                return new { PortType = (PortType?) eligiblePortTypes[0], Line = Enumerable.Range(0, 5).Select(dist => hex + dist * Hex.GetDirection(portTypeToDir[(int) eligiblePortTypes[0]])).ToArray() };
            }

            // Check if the two-step rule works for this hex
            for (int dir = 0; dir < 6; dir++)
                if (Enumerable.Range(0, 5).All(dist => (hex + 2 * dist * Hex.GetDirection(dir)).Distance < 5))
                    return new { PortType = (PortType?) null, Line = Enumerable.Range(0, 5).Select(dist => hex + 2 * dist * Hex.GetDirection(dir)).ToArray() };

            return null;
        }).Where(h => h != null);

        var inf = hexes.PickRandom();
        Debug.LogFormat("[Zoo #{0}] Line: {1} ({2})", _moduleId, inf.Line.JoinString(), inf.PortType);
    }
}
