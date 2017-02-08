/*
    GLApp.cc nachocpol@gmail.com
*/

#define GLEW_STATIC
#include "GL/glew.h"
#include <iostream>
#include "Input.h"
#include "GlApp.h"
#include "imgui.h"
#include "imgui_impl_glfw_gl3.h"
#include "gtc/matrix_transform.hpp"

GLApp::GLApp()
{
}

GLApp::~GLApp()
{
}

bool GLApp::Init()
{
    mViewport = glm::uvec4(0, 0, 1280, 1024);

    // Initialize window
    if (!mWindow.Init("Nature 3.0", glm::uvec2(mViewport.z, mViewport.w)))
    {
        printf("ERROR: Failed window creation.\n");
        return false;
    }

    // Initialize opengl
    glewExperimental = GL_TRUE;
    if (glewInit() != GLEW_OK) 
    {
        printf("ERROR: Failed to initialize opengl.\n");
        return false;
    }

    // Init IMGUI
    ImGui_ImplGlfwGL3_Init(mWindow.GetHandle(), true);

    mCamera.Init(mViewport.z / mViewport.w);
    mTerrain.Init();
    mPassConst.Init();

    // Init scene render target
    std::vector<BasicVertex> vert =
    {
        BasicVertex(-1,-1,0,    0,0),
        BasicVertex(1,-1,0,    1,0),
        BasicVertex(1, 1,0,    1,1),
        BasicVertex(-1, 1,0,    0,1)
    };
    std::vector<unsigned int> ele =
    {
        0,1,2,
        0,2,3
    };
    mBaseRt.Init(glm::vec2(mViewport.z, mViewport.w), true);
    mBaseMatRt.Init("../data/shaders/rt.vs", "../data/shaders/rt.fs");
    mBaseQuadRt.Init(vert, ele);

    // Init clouds 
    std::vector<BasicVertex> wVert =
    {
        BasicVertex(-1.0f,0.0f,-1.0f,     0.0f,0.0f),
        BasicVertex(1.0f,0.0f,-1.0f,     1.0f,0.0f),
        BasicVertex(1.0f,0.0f, 1.0f,     1.0f,1.0f),
        BasicVertex(-1.0f,0.0f, 1.0f,     0.0f,1.0f)
    };
    std::vector<unsigned int> cele =
    {
        0,1,2,
        0,2,3
    };
    mCloudsMat.Init("../data/shaders/clouds.vs",
                    "../data/shaders/clouds.fs");
    mCloudsPlane.Init(wVert, cele);

    // Init water
    mWaterReflecRt.Init(glm::vec2(mViewport.z, mViewport.w), true);
    mWaterRefracRt.Init(glm::vec2(mViewport.z, mViewport.w), true);
    mWaterMesh.Init(wVert, ele);
    mWaterMaterial.Init("../data/shaders/water.vs", "../data/shaders/water.fs");
    return true;
}

void GLApp::Update()
{
    mRunning = !mWindow.Events();

    mCamera.Update();

    mPassConst.PView = mCamera.View;
    mPassConst.PProjection = mCamera.Projection;
    mPassConst.PCamPos = mCamera.GetPosition();
    mPassConst.PTime = mTime;
    mPassConst.Update();
}

void GLApp::Render()
{
    glm::ivec2 ws;
    glfwGetWindowSize(mWindow.GetHandle(), &ws.x, &ws.y);
    if (ws.x != mViewport.z || ws.y != mViewport.w)
    {
        printf(" INFO: Window was resized(%i,%i)\n", ws.x, ws.y);
        mViewport.z = ws.x;
        mViewport.w = ws.y;
        mCamera.Resize((float)ws.x / (float)ws.y);
        // Resize render targets
        mBaseRt.Resize(ws);
        mWaterReflecRt.Resize(ws);
        mWaterRefracRt.Resize(ws);
    }

    glClearColor(0.45f, 0.75f, 0.95f, 1.0f);
    glViewport( mViewport.x, mViewport.y,
                mViewport.z, mViewport.w);
    glEnable(GL_DEPTH_TEST);
    glEnable(GL_CULL_FACE);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    // Render scene
    mBaseRt.Enable();
    {
        // Terrain
        mTerrain.Draw(false);

        // Clouds
        glDisable(GL_CULL_FACE);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        mCloudsMat.Use();
        glm::mat4 ctrans = glm::mat4();
        ctrans = glm::translate(ctrans, glm::vec3(0.0f, 500.0f, 0.0f));
        ctrans = glm::scale(ctrans, glm::vec3(1000.0f));
        glw::SetTransform(mCloudsMat.Id, &ctrans[0][0]);
        mCloudsPlane.Draw();
        glDisable(GL_BLEND);
        glEnable(GL_CULL_FACE);
    }
    mBaseRt.Disable();

    // Draw
    mBaseMatRt.Use();
    glActiveTexture(GL_TEXTURE0 + 0);
    glBindTexture(GL_TEXTURE_2D, mBaseRt.RenderTexture.Id);
    glUniform1i(glGetUniformLocation(mBaseMatRt.Id, "uColorTexture"), 0);
    mBaseQuadRt.Draw();


    RenderUi();
    mWindow.Swap();
}

void GLApp::RenderUi()
{
    ImGui_ImplGlfwGL3_NewFrame();

    ImGui::Begin("Nature 3.0");
    {
        ImGui::Text("Time:          %f", mTime);
        ImGui::Text("Delta Time:    %f", mDeltaTime);
        ImGui::Text("FPS:           %i", mFps);
        ImGui::Separator();
        ImGui::DragFloat("Camera speed", &mCamera.Speed, 0.1f, 0.01f, 4.0f);
        ImGui::DragFloat("Camera sensitivity", &mCamera.Sensitivity, 0.05f, 0.1f, 1.0f);
    }
    ImGui::End();

    ImGui::Render();
}

void GLApp::Release()
{
    ImGui_ImplGlfwGL3_Shutdown();
    mWindow.Release();
}

