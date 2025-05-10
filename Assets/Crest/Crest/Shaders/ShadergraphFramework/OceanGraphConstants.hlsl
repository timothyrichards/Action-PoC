// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

// Constants for shader graph. For example, we can force shader features when we have yet to make a keyword for it in
// shader graph.

// This file must be included before all other includes. And it must be done for every node. This is due to #ifndef
// limiting includes from being evaluated once, and we cannot specify the order because shader graph does this.

#ifndef CREST_SHADERGRAPH_CONSTANTS_H
#define CREST_SHADERGRAPH_CONSTANTS_H

// Hack: needed these somewhere without updating the graph.
static float3 _OceanPosScale0;
static float3 _OceanPosScale1;

#define _SUBSURFACESCATTERING_ON 1

#ifndef SHADERGRAPH_PREVIEW
#ifdef UNIVERSAL_PIPELINE_CORE_INCLUDED
    #define CREST_URP 1
    // Not set and _ScreenParams.zw is "1.0 + 1.0 / _ScreenParams.xy"
    #define _ScreenSize float4(_ScreenParams.xy, float2(1.0, 1.0) / _ScreenParams.xy)
#else
    // HDRP does not appear to have a reliable keyword to target.
    #define CREST_HDRP 1
#endif
#endif

#if defined(CREST_HDRP) && (SHADERPASS == SHADERPASS_FORWARD)
#define CREST_HDRP_FORWARD_PASS 1
#endif

#endif // CREST_SHADERGRAPH_CONSTANTS_H
