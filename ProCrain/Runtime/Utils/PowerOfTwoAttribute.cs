using System.Linq;
using UnityEngine;

namespace Procrain
{
    public class PowerOfTwoAttribute: PropertyAttribute
    {
        public readonly string[] optionLabels = Enumerable
            .Range(0, 12)
            .Select(x => $"{1 << x}x{1 << x}")
            .ToArray();

        public readonly int[] options = Enumerable.Range(0, 12).Select(x => 1 << x).ToArray();

        public PowerOfTwoAttribute(int min, int max, bool includeZero = false, bool label2d = false)
        {
            options = Enumerable.Range(min, max + 1 - min).Select(x => 1 << x).ToArray();
            optionLabels = Enumerable
                .Range(min, max + 1 - min)
                .Select(x => label2d ? $"{1 << x}x{1 << x}" : (1 << x).ToString())
                .ToArray();

            if (includeZero)
            {
                options = options.Prepend(0).ToArray();
                optionLabels = optionLabels.Prepend("0").ToArray();
            }
        }

        public PowerOfTwoAttribute() { }
    }
}
