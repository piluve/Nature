/*
    GLApp.h nachocpol@gmail.com
*/

#pragma once

#include "App.h"
#include "Window.h"
#include "Terrain.h"
#include "Camera.h"

class GLApp:public App
{
public:
    GLApp();
    ~GLApp();

private:
    virtual bool Init();
    virtual void Update();
    virtual void Render();
    virtual void RenderUi();
    virtual void Release();

    Window mWindow;
    glm::uvec4 mViewport;

    // Scene
    Terrain mTerrain;
    Camera mCamera;
    glw::PassConstants mPassConst;
    glw::RenderTarget mBaseRt;
    glw::Material mBaseMatRt;
    glw::Mesh mBaseQuadRt;

    // Clouds
    float mCloudsHeight = 250.0f;
    glw::Material mCloudsMat;
    glw::Mesh mCloudsPlane;

    // Water
    float mWaterHeight = 10.0f;
    float mWaterScale = 1000.0f;
    glw::RenderTarget mWaterReflecRt;
    glw::RenderTarget mWaterRefracRt;
    glw::Mesh mWaterMesh;
    glw::Material mWaterMaterial;
    glw::Texture mWaterDuDvTexture;
    glw::Texture mWaterNormTexture;
};
