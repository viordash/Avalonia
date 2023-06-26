#pragma once

#include "com.h"
#include "comimpl.h"
#include "avalonia-native.h"

@protocol IRenderTarget

-(void) resize: (AvnPixelSize) size withScale: (float) scale;
-(CALayer*) layer;

@end

@interface IOSurfaceRenderTarget : NSObject<IRenderTarget>
-(IOSurfaceRenderTarget*) initWithOpenGlContext: (IAvnGlContext*) context;
-(IAvnGlSurfaceRenderTarget*) createSurfaceRenderTarget;
-(IAvnSoftwareRenderTarget*) createSoftwareRenderTarget;
-(HRESULT) setSwFrame: (AvnFramebuffer*) fb;
@end
