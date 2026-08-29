namespace Procrain
{
    public static class ArrayExtensions
    {
        public static float[] Flatten(this float[,] array)
        {
            int length = array.GetLength(0) * array.GetLength(1);
            var result = new float[length];
            for (var i = 0; i < array.GetLength(0); i++)
            for (var j = 0; j < array.GetLength(1); j++)
                result[i * array.GetLength(1) + j] = array[i, j];
            return result;
        }
    }
}
