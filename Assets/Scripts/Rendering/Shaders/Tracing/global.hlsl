#pragma once

// frame target
RWTexture2D<float4> _FrameTarget;

// camera info
float3 _CameraPos;
float3 _CameraUp;
float3 _CameraRight;
float3 _CameraForward;
float4 _CameraInfo; // fov scale, focal distance, aperture, height / width ratio

// skybox texture
TextureCube<float4> _SkyboxTexture;
SamplerState sampler_SkyboxTexture;



