// float3 GerstnerWave(float3 position, float steepness, float wavelength, float speed, float direction, inout float3 tangent, inout float3 binormal)
// {
    //     direction = direction * 2 - 1;
    //     float2 d = normalize(float2(cos(3.14 * direction), sin(3.14 * direction)));
    //     float k = 2 * 3.14 / wavelength;                                           
    //     float f = k * (dot(d, position.xz) - speed * _Time.y);
    //     float a = steepness / k;

    //     tangent += float3(
    //     -d.x * d.x * (steepness * sin(f)),
    //     d.x * (steepness * cos(f)),
    //     -d.x * d.y * (steepness * sin(f))
    //     );

    //     binormal += float3(
    //     -d.x * d.y * (steepness * sin(f)),
    //     d.y * (steepness * cos(f)),
    //     -d.y * d.y * (steepness * sin(f))
    //     );

    //     return float3(
    //     d.x * (a * cos(f)),
    //     a * sin(f),
    //     d.y * (a * cos(f))
    //     );
// }

float3 GerstnerWave (float4 wave, float3 p, inout float3 tangent, inout float3 binormal) {
    float steepness = wave.z;
    float wavelength = wave.w;
    float k = 2 * 3.141592653589793238462 / wavelength;
    float c = sqrt(9.8 / k);
    float2 d = normalize(wave.xy);
    float f = k * (dot(d, p.xz) - c * _Time.y);
    float a = steepness / k;

    tangent += float3(
    -d.x * d.x * (steepness * sin(f)),
    d.x * (steepness * cos(f)),
    -d.x * d.y * (steepness * sin(f))
    );
    binormal += float3(
    -d.x * d.y * (steepness * sin(f)),
    d.y * (steepness * cos(f)),
    -d.y * d.y * (steepness * sin(f))
    );
    return float3(
    d.x * (a * cos(f)),
    a * sin(f),
    d.y * (a * cos(f))
    );
}

void GerstnerWaves_float(float3 position, float4 WaveA, float4 WaveB, float4 WaveC, out float3 Offset, out float3 normal)
{
    Offset = 0;
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);
    // float3 tangent = 0;
    // float3 binormal = 0;

    // Offset += GerstnerWave(position, steepness, wavelength, speed, directions.x, tangent, binormal);
    // Offset += GerstnerWave(position, steepness, wavelength, speed, directions.y, tangent, binormal);
    // Offset += GerstnerWave(position, steepness, wavelength, speed, directions.z, tangent, binormal);
    // Offset += GerstnerWave(position, steepness, wavelength, speed, directions.w, tangent, binormal);

    Offset += GerstnerWave(WaveA, position, tangent, binormal);
    Offset += GerstnerWave(WaveB, position, tangent, binormal);
    Offset += GerstnerWave(WaveC, position, tangent, binormal);

    normal = normalize(cross(binormal, tangent));
    //TBN = transpose(float3x3(tangent, binormal, normal));
}