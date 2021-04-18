void BigDither_float(float4 In, float4 ScreenPosition, out float Out) 
{
	float2 uv = ScreenPosition.xy * _ScreenParams.xy;
	float DITHER_THRESHOLDS[32] =
	{
		1.0 / 33.0,  9.0 / 33.0,  3.0 / 33.0, 11.0 / 33.0,
		13.0 / 33.0,  5.0 / 33.0, 15.0 / 33.0,  7.0 / 33.0,
		4.0 / 33.0, 12.0 / 33.0,  2.0 / 33.0, 10.0 / 33.0,
		16.0 / 33.0,  8.0 / 33.0, 14.0 / 33.0,  6.0 / 33.0,

		17.0 / 33.0,  25.0 / 33.0,  19.0 / 33.0, 27.0 / 33.0,
		29.0 / 33.0, 21.0 / 33.0, 31.0 / 33.0,  23.0 / 33.0,
		20.0 / 33.0, 28.0 / 33.0,  18.0 / 33.0, 26.0 / 33.0,
		32.0 / 33.0,  24.0 / 33.0, 30.0 / 33.0,  22.0 / 33.0
	};
	int index = (uint(uv.x) % 8) * 8 + uint(uv.y) % 8;
	Out = In - DITHER_THRESHOLDS[index];
}