/*
    Camera.h nachocpol@gmail.com
*/

#pragma once

#include "glm.hpp"
#include "Types.h"

class Camera
{
public:
    Camera();
    ~Camera();
    void Init(float aspect);
    void Resize(float aspect);
    void Update();
    void UpdateView();
    glm::vec3* GetPositionPtr() { return &mPosition; }
    glm::vec3 GetPosition() { return mPosition; }
    glm::vec3 GetFront() { return mFront; }
    glm::vec3 GetUp() { return mUp; }
    glm::vec3 GetRight() { return glm::normalize(glm::cross(mUp, mFront)); }
    void SetPosition(glm::vec3 p) { mPosition = p; }
    void SetPosition(float x, float y, float z) { mPosition = glm::vec3(x, y, z); }
    void Move(glm::vec3 p) { mPosition += p; }
    void Move(float x, float y, float z) { mPosition += glm::vec3(x, y, z); }
    void SetYInverse(bool newInv);
    float GetPitch() { return mPitch; }
    void SetPitch(float p) { mPitch = p; }
    float GetNear() { return mNear; }
    float GetFar() { return mFar; }
    float* GetFov() { return &mFovDegree; }
    void SetFov(float angle);

    glm::mat4 View;
    glm::mat4 Projection;
    Frustrum CameraFrustrum;
    float Sensitivity = 0.25f;
    float Speed = 1.0f;
    bool LockMouse = true;
	bool CameraOrbit = false;
	bool UpdateFustrum = true;

private:
    void UpdateMouseLook();
	glm::mat4 PerspectiveInverseMap(float aspect);

    glm::vec3 mUp;
    glm::vec3 mPosition;
    glm::vec3 mFront;

    bool mYInversed = true;

    float mNear;
    float mFar;
    float mFovRad;
    float mFovDegree;
    float mAspect;  // cached to use when changing fov

    // Mouse look
    float mLastX = 0.0f;
    float mLastY = 0.0f;

    float mYaw = 0.0f;
    float mPitch = 0.0f;
};