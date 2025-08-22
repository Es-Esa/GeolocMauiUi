namespace ClientApp.Core.Utils
{
	public static class MathUtils
	{
		public static float Sigmoid(float value)
		{
			var k = (float) Math.Exp(value);
			return k / (1.0f + k);
		}
	}
}
