/*
    Sky.cc nachocpol@gmail.com
*/

#include "Sky.h"
#include "glm.hpp"
#include "gtc/matrix_transform.hpp"
#include "imgui.h"
#include "GLFW/glfw3.h"

const float kPi = 3.141517f;

Sky::Sky()
{

}

Sky::~Sky()
{
}

void Sky::Init()
{
    MeshBasicVertexData md;
    LoadMeshFromFile("../data/meshes/spherehi.obj", md);
    mSkyMesh.Init(md.vertex, md.ele);
    mSkyMaterial.Init
    (
        "../data/shaders/sky/sky2.vs",
        "../data/shaders/sky/sky2.fs"
    );
}

void Sky::Render()
{
    glDisable(GL_CULL_FACE);
    glDepthMask(GL_FALSE);
    mSkyMaterial.Use();

    GLuint p = mSkyMaterial.Id;
    glw::SetUniform3f("uSunPosition",p, &SunPosition.x);
    // glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
    mSkyMesh.Render();
    // glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    glDepthMask(GL_TRUE);
    glEnable(GL_CULL_FACE);
}

void Sky::RenderUi()
{
    ImGui::Begin("Nature 3.0");
    {
        ImGui::Text("Sky");
        ImGui::Separator();
    }
    ImGui::End();
}
