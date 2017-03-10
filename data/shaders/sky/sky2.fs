#version  430 core

layout(std140)uniform uPass
{
	mat4 uView;
	mat4 uProjection;
	vec3 uCampos;
	float uTime;
	float uCamnear;
	float uCamfar;
};

uniform vec3 uSunPosition;

in vec3 iPosition;

out vec4 oColor;

void main()
{
	vec3 toAtmosphere = normalize(iPosition - vec3(0.0f));
	vec3 toSun = normalize(normalize(uSunPosition) - vec3(0.0f));

	// Sun
	float sunHeight = toSun.y;
	float sunFactor = pow(max(dot(toAtmosphere,toSun),0.0f),2500.0f);
	float sunIntensity = 5.0f;
	vec3 sunColor = vec3(1.0f,1.0f,1.0f) * sunFactor * sunIntensity;
	
	// Atmosphere
	// Basic mix of white at the horizon and bluish
	vec3 atmosphereColor = mix(vec3(1.0f,1.0f,1.0f),vec3(0.1f,0.4f,0.9f),pow(iPosition.y,0.35f));
	// Dark during the night
	atmosphereColor = mix(vec3(0.0f),atmosphereColor,sunHeight);

	oColor = vec4(sunColor + atmosphereColor,1.0f);
	if(iPosition.y <= 0.0f)oColor = vec4(0.2f);
}