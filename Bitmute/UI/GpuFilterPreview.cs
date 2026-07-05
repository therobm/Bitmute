using System;
using System.Collections.Generic;
using SkiaSharp;
using Bitmute.Imaging;

namespace Bitmute.UI
{
	public class GpuFilterPreview
	{
		public const string BoxBlurSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float radius = params.x;
	float windowLength = (2.0 * radius) + 1.0;
	float4 sum = float4(0.0);
	if (passIndex < 0.5)
	{
		for (int i = 0; i <= 200; i++)
		{
			if (float(i) <= 2.0 * radius)
			{
				float sampleX = clamp(px.x + float(i) - radius, 0.0, size.x - 1.0);
				sum += float4(src.eval(float2(sampleX + 0.5, px.y + 0.5)));
			}
		}
	}
	else
	{
		for (int i = 0; i <= 200; i++)
		{
			if (float(i) <= 2.0 * radius)
			{
				float sampleY = clamp(px.y + float(i) - radius, 0.0, size.y - 1.0);
				sum += float4(src.eval(float2(px.x + 0.5, sampleY + 0.5)));
			}
		}
	}
	return half4(sum / windowLength);
}
";

		public const string MotionBlurSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleBilinear(float2 pos)
{
	float maxX = size.x - 1.0;
	float maxY = size.y - 1.0;
	float sx = clamp(pos.x, 0.0, maxX);
	float sy = clamp(pos.y, 0.0, maxY);
	float x0 = floor(sx);
	float y0 = floor(sy);
	float fx = sx - x0;
	float fy = sy - y0;
	float x1 = min(x0 + 1.0, maxX);
	float y1 = min(y0 + 1.0, maxY);
	float4 c00 = float4(src.eval(float2(x0 + 0.5, y0 + 0.5)));
	float4 c10 = float4(src.eval(float2(x1 + 0.5, y0 + 0.5)));
	float4 c01 = float4(src.eval(float2(x0 + 0.5, y1 + 0.5)));
	float4 c11 = float4(src.eval(float2(x1 + 0.5, y1 + 0.5)));
	float w00 = (1.0 - fx) * (1.0 - fy);
	float w10 = fx * (1.0 - fy);
	float w01 = (1.0 - fx) * fy;
	float w11 = fx * fy;
	return (w00 * c00) + (w10 * c10) + (w01 * c01) + (w11 * c11);
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float angle = params.x;
	float dist = params.y;
	float sampleCount = min(dist, 32.0);
	float angleRadians = angle * kPi / 180.0;
	float2 dir = float2(cos(angleRadians), sin(angleRadians));
	float4 sum = float4(0.0);
	for (int i = 0; i < 32; i++)
	{
		if (float(i) < sampleCount)
		{
			float t = 0.0;
			if (sampleCount > 1.0)
			{
				t = (float(i) * (dist - 1.0) / (sampleCount - 1.0)) - ((dist - 1.0) / 2.0);
			}
			sum += sampleBilinear(px + (t * dir));
		}
	}
	return half4(sum / sampleCount);
}
";

		public const string RadialBlurSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleBilinear(float2 pos)
{
	float maxX = size.x - 1.0;
	float maxY = size.y - 1.0;
	float sx = clamp(pos.x, 0.0, maxX);
	float sy = clamp(pos.y, 0.0, maxY);
	float x0 = floor(sx);
	float y0 = floor(sy);
	float fx = sx - x0;
	float fy = sy - y0;
	float x1 = min(x0 + 1.0, maxX);
	float y1 = min(y0 + 1.0, maxY);
	float4 c00 = float4(src.eval(float2(x0 + 0.5, y0 + 0.5)));
	float4 c10 = float4(src.eval(float2(x1 + 0.5, y0 + 0.5)));
	float4 c01 = float4(src.eval(float2(x0 + 0.5, y1 + 0.5)));
	float4 c11 = float4(src.eval(float2(x1 + 0.5, y1 + 0.5)));
	float w00 = (1.0 - fx) * (1.0 - fy);
	float w10 = fx * (1.0 - fy);
	float w01 = (1.0 - fx) * fy;
	float w11 = fx * fy;
	return (w00 * c00) + (w10 * c10) + (w01 * c01) + (w11 * c11);
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float amount = params.x;
	float method = params.y;
	float2 center = (size - 1.0) * 0.5;
	float2 delta = px - center;
	float4 sum = float4(0.0);
	if (method < 0.5)
	{
		float radius = length(delta);
		if (radius < 0.000001)
		{
			return src.eval(coord);
		}
		float baseAngle = atan(delta.y, delta.x);
		float arcRadians = (amount / 100.0) * 25.0 * kPi / 180.0;
		for (int i = 0; i < 16; i++)
		{
			float sampleAngle = baseAngle + ((((float(i) + 0.5) / 16.0) - 0.5) * arcRadians);
			float2 pos = center + (radius * float2(cos(sampleAngle), sin(sampleAngle)));
			sum += sampleBilinear(pos);
		}
		return half4(sum / 16.0);
	}
	float strength = (amount / 100.0) * 0.2;
	for (int i = 0; i < 16; i++)
	{
		float scale = 1.0 + ((((float(i) + 0.5) / 16.0) - 0.5) * strength);
		float2 pos = center + (delta * scale);
		sum += sampleBilinear(pos);
	}
	return half4(sum / 16.0);
}
";

		public const string PinchSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kHalfPi = 1.5707963267948966;

float4 sampleTransparent(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		if (wy > 0.0 && sy >= 0.0 && sy <= size.y - 1.0)
		{
			for (int tx = 0; tx < 2; tx++)
			{
				float wx = 1.0 - fx;
				float sx = baseX;
				if (tx == 1)
				{
					wx = fx;
					sx = baseX + 1.0;
				}
				if (wx > 0.0 && sx >= 0.0 && sx <= size.x - 1.0)
				{
					sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
				}
			}
		}
	}
	return sum;
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float amount = params.x / 100.0;
	float cx = (size.x - 1.0) * 0.5;
	float cy = (size.y - 1.0) * 0.5;
	float rx = max(cx, 0.5);
	float ry = max(cy, 0.5);
	float dx = px.x - cx;
	float dy = px.y - cy;
	float nx = dx / rx;
	float ny = dy / ry;
	float dist = sqrt((nx * nx) + (ny * ny));
	float2 srcPos = px;
	if (dist > 0.000001 && dist < 1.0)
	{
		float curve = sin(kHalfPi * dist);
		float factor = pow(curve, -amount);
		srcPos = float2(cx + (dx * factor), cy + (dy * factor));
	}
	return half4(sampleTransparent(srcPos));
}
";

		public const string PolarCoordinatesSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleTransparent(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		if (wy > 0.0 && sy >= 0.0 && sy <= size.y - 1.0)
		{
			for (int tx = 0; tx < 2; tx++)
			{
				float wx = 1.0 - fx;
				float sx = baseX;
				if (tx == 1)
				{
					wx = fx;
					sx = baseX + 1.0;
				}
				if (wx > 0.0 && sx >= 0.0 && sx <= size.x - 1.0)
				{
					sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
				}
			}
		}
	}
	return sum;
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float cx = (size.x - 1.0) * 0.5;
	float cy = (size.y - 1.0) * 0.5;
	float rx = max(cx, 0.5);
	float ry = max(cy, 0.5);
	float spanX = max(size.x - 1.0, 1.0);
	float spanY = max(size.y - 1.0, 1.0);
	float2 srcPos = px;
	if (params.x < 0.5)
	{
		float nx = (px.x - cx) / rx;
		float ny = (px.y - cy) / ry;
		float radius = sqrt((nx * nx) + (ny * ny));
		float angle = kPi;
		if (nx != 0.0 || ny != 0.0)
		{
			angle = atan(nx, -ny);
		}
		srcPos = float2(((angle + kPi) / (2.0 * kPi)) * spanX, radius * spanY);
	}
	else
	{
		float angle = -kPi + ((2.0 * kPi) * (px.x / spanX));
		float radius = px.y / spanY;
		srcPos = float2(cx + (radius * rx * sin(angle)), cy - (radius * ry * cos(angle)));
	}
	return half4(sampleTransparent(srcPos));
}
";

		public const string RippleSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleClamp(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		sy = clamp(sy, 0.0, size.y - 1.0);
		for (int tx = 0; tx < 2; tx++)
		{
			float wx = 1.0 - fx;
			float sx = baseX;
			if (tx == 1)
			{
				wx = fx;
				sx = baseX + 1.0;
			}
			sx = clamp(sx, 0.0, size.x - 1.0);
			sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
		}
	}
	return sum;
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float amount = params.x / 100.0;
	float waveLen = 32.0;
	if (params.y < 0.5)
	{
		waveLen = 12.0;
	}
	if (params.y > 1.5)
	{
		waveLen = 64.0;
	}
	float srcX = px.x + (amount * sin((2.0 * kPi) * (px.y / waveLen)));
	float srcY = px.y + (amount * sin((2.0 * kPi) * (px.x / waveLen)));
	return half4(sampleClamp(float2(srcX, srcY)));
}
";

		public const string ShearSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleShear(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		sy = clamp(sy, 0.0, size.y - 1.0);
		for (int tx = 0; tx < 2; tx++)
		{
			float wx = 1.0 - fx;
			float sx = baseX;
			if (tx == 1)
			{
				wx = fx;
				sx = baseX + 1.0;
			}
			if (params.y < 0.5)
			{
				sx = mod(sx, size.x);
			}
			else
			{
				sx = clamp(sx, 0.0, size.x - 1.0);
			}
			sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
		}
	}
	return sum;
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float amplitude = (params.x / 100.0) * (size.x / 4.0);
	float spanY = max(size.y, 1.0);
	float srcX = px.x + (amplitude * sin(kPi * (px.y / spanY)));
	return half4(sampleShear(float2(srcX, px.y)));
}
";

		public const string SpherizeSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;
const float kHalfPi = 1.5707963267948966;

float4 sampleTransparent(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		if (wy > 0.0 && sy >= 0.0 && sy <= size.y - 1.0)
		{
			for (int tx = 0; tx < 2; tx++)
			{
				float wx = 1.0 - fx;
				float sx = baseX;
				if (tx == 1)
				{
					wx = fx;
					sx = baseX + 1.0;
				}
				if (wx > 0.0 && sx >= 0.0 && sx <= size.x - 1.0)
				{
					sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
				}
			}
		}
	}
	return sum;
}

float spherizeFactor(float dist, float amount)
{
	float mapped = dist;
	if (amount >= 0.0)
	{
		float sphere = asin(dist) * (2.0 / kPi);
		mapped = dist + (amount * (sphere - dist));
	}
	else
	{
		float sphere = sin(dist * kHalfPi);
		mapped = dist + ((-amount) * (sphere - dist));
	}
	return mapped / dist;
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float amount = params.x / 100.0;
	float mode = params.y;
	float cx = (size.x - 1.0) * 0.5;
	float cy = (size.y - 1.0) * 0.5;
	float rx = max(cx, 0.5);
	float ry = max(cy, 0.5);
	float2 srcPos = px;
	if (mode > 0.5 && mode < 1.5)
	{
		float dx = px.x - cx;
		float dist = abs(dx) / rx;
		if (dist > 0.000001 && dist <= 1.0)
		{
			float factor = spherizeFactor(dist, amount);
			srcPos = float2(cx + (dx * factor), px.y);
		}
	}
	else if (mode > 1.5)
	{
		float dy = px.y - cy;
		float dist = abs(dy) / ry;
		if (dist > 0.000001 && dist <= 1.0)
		{
			float factor = spherizeFactor(dist, amount);
			srcPos = float2(px.x, cy + (dy * factor));
		}
	}
	else
	{
		float dx = px.x - cx;
		float dy = px.y - cy;
		float nx = dx / rx;
		float ny = dy / ry;
		float dist = sqrt((nx * nx) + (ny * ny));
		if (dist > 0.000001 && dist < 1.0)
		{
			float factor = spherizeFactor(dist, amount);
			srcPos = float2(cx + (dx * factor), cy + (dy * factor));
		}
	}
	return half4(sampleTransparent(srcPos));
}
";

		public const string TwirlSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleTransparent(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		if (wy > 0.0 && sy >= 0.0 && sy <= size.y - 1.0)
		{
			for (int tx = 0; tx < 2; tx++)
			{
				float wx = 1.0 - fx;
				float sx = baseX;
				if (tx == 1)
				{
					wx = fx;
					sx = baseX + 1.0;
				}
				if (wx > 0.0 && sx >= 0.0 && sx <= size.x - 1.0)
				{
					sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
				}
			}
		}
	}
	return sum;
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float rotAmount = (params.x * kPi) / 180.0;
	float cx = (size.x - 1.0) * 0.5;
	float cy = (size.y - 1.0) * 0.5;
	float rx = max(cx, 0.5);
	float ry = max(cy, 0.5);
	float dx = px.x - cx;
	float dy = px.y - cy;
	float nx = dx / rx;
	float ny = dy / ry;
	float dist = sqrt((nx * nx) + (ny * ny));
	float2 srcPos = px;
	if (dist < 1.0)
	{
		float falloff = 1.0 - dist;
		float rotation = rotAmount * falloff * falloff;
		float cosine = cos(rotation);
		float sine = sin(rotation);
		srcPos = float2(cx + ((dx * cosine) - (dy * sine)), cy + ((dx * sine) + (dy * cosine)));
	}
	return half4(sampleTransparent(srcPos));
}
";

		public const string WaveSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

const float kPi = 3.141592653589793;

float4 sampleClamp(float2 pos)
{
	float baseX = floor(pos.x);
	float baseY = floor(pos.y);
	float fx = pos.x - baseX;
	float fy = pos.y - baseY;
	float4 sum = float4(0.0);
	for (int ty = 0; ty < 2; ty++)
	{
		float wy = 1.0 - fy;
		float sy = baseY;
		if (ty == 1)
		{
			wy = fy;
			sy = baseY + 1.0;
		}
		sy = clamp(sy, 0.0, size.y - 1.0);
		for (int tx = 0; tx < 2; tx++)
		{
			float wx = 1.0 - fx;
			float sx = baseX;
			if (tx == 1)
			{
				wx = fx;
				sx = baseX + 1.0;
			}
			sx = clamp(sx, 0.0, size.x - 1.0);
			sum += (wx * wy) * float4(src.eval(float2(sx + 0.5, sy + 0.5)));
		}
	}
	return sum;
}

float waveValue(float waveType, float t)
{
	if (waveType > 0.5 && waveType < 1.5)
	{
		float fraction = fract(t);
		if (fraction < 0.25)
		{
			return 4.0 * fraction;
		}
		if (fraction < 0.75)
		{
			return 2.0 - (4.0 * fraction);
		}
		return (4.0 * fraction) - 4.0;
	}
	if (waveType > 1.5)
	{
		float fraction = fract(t);
		if (fraction < 0.5)
		{
			return 1.0;
		}
		return -1.0;
	}
	return sin((2.0 * kPi) * t);
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float waveLen = max(params.x, 1.0);
	float amplitude = params.y;
	float waveType = params.z;
	float srcX = px.x + (amplitude * waveValue(waveType, px.y / waveLen));
	float srcY = px.y + (amplitude * waveValue(waveType, (px.x / waveLen) + 0.25));
	return half4(sampleClamp(float2(srcX, srcY)));
}
";

		public const string DiffuseSource = @"
uniform shader src;
uniform shader offsets;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

float weightedLuma(float4 color)
{
	float red = floor((color.r * 255.0) + 0.5);
	float green = floor((color.g * 255.0) + 0.5);
	float blue = floor((color.b * 255.0) + 0.5);
	return (299.0 * red) + (587.0 * green) + (114.0 * blue);
}

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float4 offsetSample = float4(offsets.eval(float2(px.x + 0.5, px.y + 0.5)));
	float code = floor((offsetSample.r * 255.0) + 0.5);
	float dyOff = floor(code / 5.0);
	float dxOff = code - (dyOff * 5.0);
	float sampleX = clamp(px.x + dxOff - 2.0, 0.0, size.x - 1.0);
	float sampleY = clamp(px.y + dyOff - 2.0, 0.0, size.y - 1.0);
	float4 candidate = float4(src.eval(float2(sampleX + 0.5, sampleY + 0.5)));
	float4 current = float4(src.eval(float2(px.x + 0.5, px.y + 0.5)));
	bool take = true;
	if (params.x > 0.5 && params.x < 1.5)
	{
		take = weightedLuma(candidate) < weightedLuma(current);
	}
	if (params.x > 1.5)
	{
		take = weightedLuma(candidate) > weightedLuma(current);
	}
	float4 chosen = current;
	if (take)
	{
		chosen = candidate;
	}
	return half4(float4(chosen.rgb * chosen.a, chosen.a));
}
";

		public const string EmbossSource = @"
uniform shader src;
uniform float2 size;
uniform float4 params;
uniform float passIndex;

half4 main(float2 coord)
{
	float2 px = floor(coord);
	float sampleX = clamp(px.x + params.x, 0.0, size.x - 1.0);
	float sampleY = clamp(px.y + params.y, 0.0, size.y - 1.0);
	float4 current = float4(src.eval(float2(px.x + 0.5, px.y + 0.5)));
	float4 offsetPixel = float4(src.eval(float2(sampleX + 0.5, sampleY + 0.5)));
	float strength = params.z / 100.0;
	float red = clamp((128.0 + (strength * ((current.r * 255.0) - (offsetPixel.r * 255.0)))) / 255.0, 0.0, 1.0);
	float green = clamp((128.0 + (strength * ((current.g * 255.0) - (offsetPixel.g * 255.0)))) / 255.0, 0.0, 1.0);
	float blue = clamp((128.0 + (strength * ((current.b * 255.0) - (offsetPixel.b * 255.0)))) / 255.0, 0.0, 1.0);
	float alpha = current.a;
	return half4(float4(red * alpha, green * alpha, blue * alpha, alpha));
}
";

		private static Dictionary<string, SKRuntimeEffect> s_effectCache = new Dictionary<string, SKRuntimeEffect>();

		private SKImage m_sourceImage;
		private SKBitmap m_targetBitmap;
		private SKBitmap m_offsetMapBitmap;
		private SKImage m_offsetMapImage;
		private SKSurface m_scratchA;
		private SKSurface m_scratchB;
		private GRRecordingContext m_scratchContext;
		private int m_scratchWidth;
		private int m_scratchHeight;
		private bool m_sessionActive;
		private bool m_pending;
		private string m_pendingSource;
		private int m_pendingPasses;
		private bool m_pendingBuiltinBlur;
		private bool m_pendingRawSource;
		private int[] m_pendingValues;

		private static SKRuntimeEffect GetEffect(string skslSource)
		{
			if (skslSource == null)
			{
				return null;
			}
			SKRuntimeEffect cached;
			if (s_effectCache.TryGetValue(skslSource, out cached))
			{
				return cached;
			}
			string errors;
			SKRuntimeEffect compiled = SKRuntimeEffect.CreateShader(skslSource, out errors);
			s_effectCache[skslSource] = compiled;
			return compiled;
		}

		public static unsafe void BuildDiffuseOffsets(SKBitmap target, int seed)
		{
			int width = target.Width;
			int height = target.Height;
			byte* targetBase = (byte*)target.GetPixels().ToPointer();
			int rowBytes = target.RowBytes;
			for (int y = 0; y < height; y++)
			{
				byte* row = targetBase + ((long)y * rowBytes);
				for (int x = 0; x < width; x++)
				{
					uint hash = FilterStylize.HashCoordinates(x, y, seed);
					int dx = (int)(hash % 5u);
					int dy = (int)((hash / 5u) % 5u);
					int pixelOffset = x * 4;
					row[pixelOffset + 0] = (byte)(dx + (5 * dy));
					row[pixelOffset + 1] = 0;
					row[pixelOffset + 2] = 0;
					row[pixelOffset + 3] = 255;
				}
			}
		}

		private static bool ReadSurface(SKSurface surface, SKBitmap destination)
		{
			SKImageInfo readInfo = new SKImageInfo(destination.Width, destination.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			bool success = surface.ReadPixels(readInfo, destination.GetPixels(), destination.RowBytes, 0, 0);
			return success;
		}

		public static bool CanRun(string skslSource)
		{
			SKRuntimeEffect effect = GetEffect(skslSource);
			if (effect == null)
			{
				return false;
			}
			return true;
		}

		public static bool RunEffect(SKSurface scratchA, SKSurface scratchB, SKImage source, string skslSource, int passes, bool builtinBlur, bool rawSource, SKImage offsetMap, int[] values, SKBitmap destination)
		{
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			if (builtinBlur)
			{
				double sigma = Math.Sqrt((double)values[0] * (values[0] + 1));
				SKImageFilter blurFilter = SKImageFilter.CreateBlur((float)sigma, (float)sigma, SKShaderTileMode.Clamp);
				SKPaint blurPaint = new SKPaint();
				blurPaint.ImageFilter = blurFilter;
				scratchA.Canvas.Clear(SKColors.Transparent);
				scratchA.Canvas.DrawImage(source, 0f, 0f, sampling, blurPaint);
				blurPaint.Dispose();
				blurFilter.Dispose();
				return ReadSurface(scratchA, destination);
			}
			SKRuntimeEffect effect = GetEffect(skslSource);
			if (effect == null)
			{
				return false;
			}
			float[] sizeUniform = new float[2];
			sizeUniform[0] = destination.Width;
			sizeUniform[1] = destination.Height;
			float[] paramsUniform = new float[4];
			int valueCount = 0;
			if (values != null)
			{
				valueCount = values.Length;
			}
			if (valueCount > 4)
			{
				valueCount = 4;
			}
			for (int index = 0; index < valueCount; index++)
			{
				paramsUniform[index] = values[index];
			}
			SKSurface finalSurface = scratchA;
			SKImage intermediate = null;
			for (int passIndex = 0; passIndex < passes; passIndex++)
			{
				SKImage passSource = source;
				SKSurface passTarget = scratchA;
				if (passIndex == 1)
				{
					intermediate = scratchA.Snapshot();
					passSource = intermediate;
					passTarget = scratchB;
				}
				SKShader sourceShader;
				if (rawSource)
				{
					sourceShader = passSource.ToRawShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling);
				}
				else
				{
					sourceShader = passSource.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling);
				}
				SKRuntimeEffectUniforms uniforms = new SKRuntimeEffectUniforms(effect);
				uniforms["size"] = sizeUniform;
				uniforms["params"] = paramsUniform;
				uniforms["passIndex"] = (float)passIndex;
				SKRuntimeEffectChildren children = new SKRuntimeEffectChildren(effect);
				children["src"] = sourceShader;
				SKShader offsetShader = null;
				if (offsetMap != null)
				{
					offsetShader = offsetMap.ToRawShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling);
					children["offsets"] = offsetShader;
				}
				SKShader effectShader = effect.ToShader(uniforms, children);
				SKPaint paint = new SKPaint();
				paint.Shader = effectShader;
				passTarget.Canvas.Clear(SKColors.Transparent);
				passTarget.Canvas.DrawPaint(paint);
				paint.Dispose();
				effectShader.Dispose();
				children.Dispose();
				uniforms.Dispose();
				if (offsetShader != null)
				{
					offsetShader.Dispose();
				}
				sourceShader.Dispose();
				finalSurface = passTarget;
			}
			if (intermediate != null)
			{
				intermediate.Dispose();
			}
			return ReadSurface(finalSurface, destination);
		}

		public void BeginSession(SKBitmap snapshot, SKBitmap targetLayerBitmap)
		{
			if (m_sourceImage != null)
			{
				m_sourceImage.Dispose();
				m_sourceImage = null;
			}
			m_sourceImage = SKImage.FromPixels(snapshot.PeekPixels());
			m_targetBitmap = targetLayerBitmap;
			m_sessionActive = true;
		}

		public void SetSessionOffsetMap(SKBitmap offsetMap)
		{
			if (m_offsetMapImage != null)
			{
				m_offsetMapImage.Dispose();
				m_offsetMapImage = null;
			}
			if (m_offsetMapBitmap != null)
			{
				m_offsetMapBitmap.Dispose();
				m_offsetMapBitmap = null;
			}
			m_offsetMapBitmap = offsetMap;
			m_offsetMapImage = SKImage.FromPixels(offsetMap.PeekPixels());
		}

		public bool SessionActive()
		{
			return m_sessionActive;
		}

		public void QueuePending(string skslSource, int passes, bool builtinBlur, bool rawSource, int[] values)
		{
			int count = 0;
			if (values != null)
			{
				count = values.Length;
			}
			int[] copy = new int[count];
			for (int index = 0; index < count; index++)
			{
				copy[index] = values[index];
			}
			m_pendingSource = skslSource;
			m_pendingPasses = passes;
			m_pendingBuiltinBlur = builtinBlur;
			m_pendingRawSource = rawSource;
			m_pendingValues = copy;
			m_pending = true;
		}

		public bool HasPending()
		{
			return m_pending;
		}

		public void ClearPending()
		{
			m_pending = false;
			m_pendingSource = null;
			m_pendingPasses = 0;
			m_pendingBuiltinBlur = false;
			m_pendingRawSource = false;
			m_pendingValues = null;
		}

		public void EndSession()
		{
			ClearPending();
			if (m_sourceImage != null)
			{
				m_sourceImage.Dispose();
				m_sourceImage = null;
			}
			if (m_offsetMapImage != null)
			{
				m_offsetMapImage.Dispose();
				m_offsetMapImage = null;
			}
			if (m_offsetMapBitmap != null)
			{
				m_offsetMapBitmap.Dispose();
				m_offsetMapBitmap = null;
			}
			if (m_scratchA != null)
			{
				m_scratchA.Dispose();
				m_scratchA = null;
			}
			if (m_scratchB != null)
			{
				m_scratchB.Dispose();
				m_scratchB = null;
			}
			m_scratchContext = null;
			m_scratchWidth = 0;
			m_scratchHeight = 0;
			m_targetBitmap = null;
			m_sessionActive = false;
		}

		public bool RunPending(GRRecordingContext context)
		{
			if (!m_sessionActive)
			{
				return false;
			}
			if (!m_pending)
			{
				return false;
			}
			if (context == null)
			{
				return false;
			}
			if (m_sourceImage == null)
			{
				return false;
			}
			if (m_targetBitmap == null)
			{
				return false;
			}
			if (!m_pendingBuiltinBlur && m_pendingSource == null)
			{
				ClearPending();
				return false;
			}
			if (m_pendingValues == null)
			{
				ClearPending();
				return false;
			}
			int width = m_targetBitmap.Width;
			int height = m_targetBitmap.Height;
			bool recreate = false;
			if (m_scratchA == null)
			{
				recreate = true;
			}
			if (m_scratchB == null)
			{
				recreate = true;
			}
			if (!ReferenceEquals(m_scratchContext, context))
			{
				recreate = true;
			}
			if (m_scratchWidth != width)
			{
				recreate = true;
			}
			if (m_scratchHeight != height)
			{
				recreate = true;
			}
			if (recreate)
			{
				if (m_scratchA != null)
				{
					m_scratchA.Dispose();
					m_scratchA = null;
				}
				if (m_scratchB != null)
				{
					m_scratchB.Dispose();
					m_scratchB = null;
				}
				SKImageInfo scratchInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
				m_scratchA = SKSurface.Create(context, true, scratchInfo);
				m_scratchB = SKSurface.Create(context, true, scratchInfo);
				m_scratchContext = context;
				m_scratchWidth = width;
				m_scratchHeight = height;
			}
			if (m_scratchA == null)
			{
				ClearPending();
				return false;
			}
			if (m_scratchB == null)
			{
				ClearPending();
				return false;
			}
			bool result = RunEffect(m_scratchA, m_scratchB, m_sourceImage, m_pendingSource, m_pendingPasses, m_pendingBuiltinBlur, m_pendingRawSource, m_offsetMapImage, m_pendingValues, m_targetBitmap);
			ClearPending();
			return result;
		}
	}
}
