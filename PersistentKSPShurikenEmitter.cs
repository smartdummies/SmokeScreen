﻿/*
 * Copyright (c) 2017, Sébastien GAGGINI AKA Sarbian, France
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 */

using System;
using SmokeScreen;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

// TODO : handle the relation with PersistentEmitterManager inside the class
public class PersistentKSPShurikenEmitter
{
    public GameObject go;

    public ParticleSystem pe;
    public ParticleSystemRenderer pr;

    public bool fixedEmit;

    public float endTime;

    public double particleFraction;

    public readonly float minEmissionBase;
    public readonly float maxEmissionBase;
    public readonly float minEnergyBase;
    public readonly float maxEnergyBase;
    public readonly float minSizeBase;
    public readonly float maxSizeBase;
    public readonly float scale1DBase;
    public readonly Vector2 scale2DBase;
    public readonly Vector3 scale3DBase;
    public readonly Vector3 localVelocityBase;
    public readonly Vector3 worldVelocityBase;
    public readonly Vector3 forceBase;

    public KSPParticleEmitter.EmissionShape shape;

    public float sizeGrow;
    public float minEmission;
    public float maxEmission;
    public float minEnergy;
    public float maxEnergy;
    public float minSize;
    public float maxSize;
    public float shape1D;
    public Vector2 shape2D;
    public Vector3 shape3D;
    public Vector3 localVelocity;
    public Vector3 worldVelocity;
    public Vector3 force;
    public Vector3 rndForce;
    public Color color;
    public bool doesAnimateColor;
    public Color[] colors;

    //private float emitterVelocityScale;
    private Vector3 rndVelocity;
    private bool rndRotation;
    private float angularVelocity;
    private float rndAngularVelocity;

    public float logarithmicGrow;

    public float logarithmicGrowScale;

    public float linearGrow;

    public float sizeClamp = 50;

    public bool clampXYstart = false;

    // The initial velocity of the particles will be offset by a random amount
    // lying in a disk perpendicular to the mean initial velocity whose radius
    // is randomOffsetMaxRadius. This is similar to Unity's 'Random Velocity'
    // Setting, except it will sample the offset from a (normal) disk rather
    // than from a cube. Units (SI): m/s.
    public float randomInitalVelocityOffsetMaxRadius = 0.0f;

    //Similar to randomInitalVelocityOffsetMaxRadius, cleaned a little 
    public float randConeEmit = 0.0f;

    //Additive random position offset
    public float vRandPosOffset = 0.0f;

    //Additive non-random position offset
    public float vPosOffset = 0.0f;

    //xyForce multiplicatively damps non-axis (x,y) motion, leaving axis 
    //motion (z) untouched.
    public float xyForce = 1.0f;

    //zForce is like xyForce, but directly adjusts in the direction of motion,
    public float zForce = 1.0f;

    // Whether to apply Archimedes' force, gravity and other things to the
    // particle.
    public bool physical = false;

    // Initial density of the particle seen as sphere of radius size of perfect
    // gas. We then assume (only true for ideally expanded exhaust) that the
    // expansion is isobaric (by mixing with the atmosphere) in order to compute
    // the density afterwards. Units (SI): kg / m^3.
    public double initialDensity = .6;

    public double dragCoefficient = 0.1;

    // How much the particles stick to objects they collide with.
    public double stickiness = 0.9;

    public bool collide = false;

    public float collideRatio = 0.0f;

    private bool addedLaunchPadCollider;

    private static uint physicsPass = 4;

    private static uint activePhysicsPass;

    private static ParticleSystem.Particle[] particles;

    public PersistentKSPShurikenEmitter(
        GameObject go,
        ParticleSystem pe,
        ParticleSystemRenderer pr,
        KSPParticleEmitter templateKspParticleEmitter)
    {
        this.go = go;
        this.pe = pe;
        this.pr = pr;

        // TODO That s what we need to also save to emit manually with proper values

        // float emitterVelocityScale
        // Vector3 rndVelocity
        // bool rndRotation
        // float angularVelocity
        // float rndAngularVelocity 

        //templateKspParticleEmitter.ve

        shape = templateKspParticleEmitter.shape;

        scale1DBase = shape1D = templateKspParticleEmitter.shape1D;
        scale2DBase = shape2D = templateKspParticleEmitter.shape2D;
        scale3DBase = shape3D = templateKspParticleEmitter.shape3D;

        minEmissionBase = minEmission = templateKspParticleEmitter.minEmission;
        maxEmissionBase = maxEmission = templateKspParticleEmitter.maxEmission;
        minEnergyBase = minEnergy = templateKspParticleEmitter.minEnergy;
        maxEnergyBase = maxEnergy = templateKspParticleEmitter.maxEnergy;

        minSizeBase = minSize = templateKspParticleEmitter.minSize;
        maxSizeBase = maxSize = templateKspParticleEmitter.maxSize;

        localVelocityBase = localVelocity = templateKspParticleEmitter.localVelocity;
        worldVelocityBase = worldVelocity = templateKspParticleEmitter.worldVelocity;

        forceBase = force = templateKspParticleEmitter.force;
        rndForce = templateKspParticleEmitter.rndForce;

        rndVelocity = templateKspParticleEmitter.rndVelocity;
        rndRotation = templateKspParticleEmitter.rndRotation;
        angularVelocity = templateKspParticleEmitter.angularVelocity;
        rndAngularVelocity = templateKspParticleEmitter.rndAngularVelocity;

        // Unity sure love its strange way of using struct (this actually works because each properties of the struct does magic)
        ParticleSystem.ForceOverLifetimeModule fol = pe.forceOverLifetime;
        fol.enabled = force.sqrMagnitude > 0 || rndForce.sqrMagnitude > 0;
        fol.x = new ParticleSystem.MinMaxCurve(forceBase.x, forceBase.x + rndForce.x);
        fol.y = new ParticleSystem.MinMaxCurve(forceBase.y, forceBase.y + rndForce.y);
        fol.z = new ParticleSystem.MinMaxCurve(forceBase.z, forceBase.z + rndForce.z);

        color = templateKspParticleEmitter.color;
        
        PersistentEmitterManager.Add(this);
    }

    // Detach the emitter from its parent gameobject and stop its emmission in timer seconds
    public void Detach(float timer)
    {
        //Print("Detach");
        endTime = Time.fixedTime + timer;
        if (go != null && go.transform.parent != null)
        {
            // detach from the parent so the emmitter(and its particle) don't get removed instantly
            go.transform.parent = null;
        }
    }

    public void EmissionStop()
    {
        fixedEmit = false;
        if (pe != null)
        {
            ParticleSystem.EmissionModule em = pe.emission;
            em.enabled = false;
        }
    }

    private void Emit()
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        
        Vector3 pos = Vector3.zero;

        switch (shape)
        {
            case KSPParticleEmitter.EmissionShape.Point:
                pos = Vector3.zero;
                break;

            case KSPParticleEmitter.EmissionShape.Line:
                pos = new Vector3(Random.Range(-shape1D, shape1D) * 0.5f, 0f, 0f);
                break;

            case KSPParticleEmitter.EmissionShape.Ellipsoid:
                pos = Random.insideUnitSphere;
                pos.Scale(shape3D);
                break;

            case KSPParticleEmitter.EmissionShape.Ellipse:
                pos = Random.insideUnitCircle;
                pos.x = pos.x * shape2D.x;
                pos.z = pos.y * shape2D.y;
                pos.y = 0f;
                break;

            case KSPParticleEmitter.EmissionShape.Sphere:
                pos = Random.insideUnitSphere * shape1D;
                break;

            case KSPParticleEmitter.EmissionShape.Cuboid:
                pos = new Vector3(
                    Random.Range(-shape3D.x, shape3D.x),
                    Random.Range(-shape3D.y, shape3D.y),
                    Random.Range(-shape3D.z, shape3D.z));
                break;

            case KSPParticleEmitter.EmissionShape.Plane:
                pos = new Vector3(Random.Range(-shape2D.x, shape2D.x), 0f, Random.Range(-shape2D.y, shape2D.y));
                break;

            case KSPParticleEmitter.EmissionShape.Ring:
                float posFloat = Random.Range(0f, 2f * Mathf.PI);
                pos = new Vector3(Mathf.Sin(posFloat) * shape2D.x, 0f, Mathf.Cos(posFloat) * shape2D.y);
                break;
        }

        Vector3 vel;
        if (pe.main.simulationSpace == ParticleSystemSimulationSpace.Local)
        {
            vel = localVelocity + new Vector3(
                      Random.Range(-rndVelocity.x, rndVelocity.x),
                      Random.Range(-rndVelocity.y, rndVelocity.y),
                      Random.Range(-rndVelocity.z, rndVelocity.z)) +
                  go.transform.InverseTransformDirection(worldVelocity);
        }
        else
        {
            pos = go.transform.TransformPoint(pos);
            vel = worldVelocity 
                + go.transform.TransformDirection(
                    localVelocity 
                    + new Vector3(
                        Random.Range(-rndVelocity.x, rndVelocity.x),
                        Random.Range(-rndVelocity.y, rndVelocity.y),
                        Random.Range(-rndVelocity.z, rndVelocity.z)
                        )
                    );
        }

        float rotation = rndRotation ? Random.value * 360f : 0f;
        float angularV = angularVelocity + Random.value * rndAngularVelocity;

        emitParams.position = pos;
        emitParams.velocity = vel;
        emitParams.rotation = rotation;
        emitParams.angularVelocity = angularV;
        emitParams.startLifetime = Random.Range(minEnergy, maxEnergy);
        emitParams.startColor = color;
        emitParams.startSize = Random.Range(minSize, maxSize);
        
        pe.Emit(emitParams, 1);
    }
    
    // Update the particles of the Emitter : Emit, resize, collision and physic
    public void EmitterOnUpdate(Vector3 emitterWorldVelocity)
    {
        if (pe == null)
            return;


        // "Default", "TransparentFX", "Local Scenery", "Ignore Raycast"
        int mask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("Local Scenery"));

        Profiler.BeginSample("fixedEmit");
        // Emit particles on fixedUpdate rather than Update so that we know which particles
        // were just created and should be nudged, should not be collided, etc.
        if (fixedEmit)
        {
            // Number of particles to emit:
            double averageEmittedParticles = Random.Range(minEmission, maxEmission)
                                             * TimeWarp.fixedDeltaTime;
            double compensatedEmittedParticles = averageEmittedParticles + particleFraction;
            double emittedParticles = Math.Truncate(compensatedEmittedParticles);
            particleFraction = compensatedEmittedParticles - emittedParticles;

            int emissionCount = (int)emittedParticles;
            for (int k = 0; k < emissionCount; ++k)
            {
                Emit();
            }
        }
        Profiler.EndSample();

        if (particles == null || pe.main.maxParticles > particles.Length)
            particles = new ParticleSystem.Particle[pe.main.maxParticles];
        
        Profiler.BeginSample("GetParticles");
        int numParticlesAlive = pe.GetParticles(particles);
        Profiler.EndSample();

        double averageSize = 0.5 * (minSize + maxSize);
        
        //For randConeEmit:
        //Only generate a random vector on every other particle, for the in-between particles, negate the disk.
        bool toggle = true;
        Vector2 disk = new Vector2 (0,0);
        //For startSpread

        double logGrowConst = TimeWarp.fixedDeltaTime * logarithmicGrow * logarithmicGrowScale;
        float linGrowConst = (float)(TimeWarp.fixedDeltaTime * linearGrow * averageSize);
        float growConst = Mathf.Pow( 1 + sizeGrow, TimeWarp.fixedDeltaTime);

        Transform peTransform = pe.transform;

        Vector3d frameVel = Krakensbane.GetFrameVelocity();

        bool useWorldSpace = pe.main.simulationSpace == ParticleSystemSimulationSpace.World;

        Profiler.BeginSample("Loop");

        //Step through all the particles:
        for (int j = 0; j < numParticlesAlive; j++)
        {
            ParticleSystem.Particle particle = particles[j];

            Profiler.BeginSample("Cull");
            // Check if we need to cull the number of particles
            if (SmokeScreenConfig.particleDecimate != 0 && numParticlesAlive > SmokeScreenConfig.decimateFloor)
            {
                SmokeScreenConfig.particleCounter++;
                if ((SmokeScreenConfig.particleDecimate > 0
                     && (SmokeScreenConfig.particleCounter % SmokeScreenConfig.particleDecimate) == 0)
                    || (SmokeScreenConfig.particleDecimate < 0
                        && (SmokeScreenConfig.particleCounter % SmokeScreenConfig.particleDecimate) != 0))
                {
                    particle.remainingLifetime = 0; // energy set to 0 remove the particle, as per Unity doc
                }
            }
            Profiler.EndSample();

            if (particle.remainingLifetime > 0)
            {
                //Slight methodology change to avoid duplicating if statements:
                Vector3d pVel;
                Vector3d pPos;
                Profiler.BeginSample("lifetime");
                if (useWorldSpace)
                {
                    pVel = particle.velocity + frameVel;
                    pPos = particle.position;
                }
                else if (!useWorldSpace && particle.remainingLifetime == particle.startLifetime)
                {
                    Vector3 lVel = new Vector3(0, 0, 1);
                    Vector3 lPos = particle.position;

                    // Adjust initial velocity to make a cone.  Only perform if pe.useWorldSpace
                    // is true, and we have a randConeEmit set.
                    //Produce a random vector within "angle" of the original vector.
                    //The maximum producible cone is 90 degrees when randConeEmit is very large.
                    //Could open up more if we used trig, but it'd be less efficient.

                    if (toggle)
                    {
                        disk = Random.insideUnitCircle * randConeEmit;
                        toggle = false;
                    }
                    else
                    {
                        disk *= -1;
                        toggle = true;
                    }
                    lVel.x = disk.x;
                    lVel.y = disk.y;
                    lVel = Vector3.Normalize(lVel);
                    lVel *= Vector3.Magnitude(particle.velocity);

                    //Adjust initial position back along its position, if required.
                    //Apply a random offset if vRandOffset != 0, else apply zero.
                    float randoff = (vRandPosOffset != 0)? Random.Range(0, vRandPosOffset) : 0;
                    lPos += Vector3.Normalize(lVel) * (randoff + vPosOffset);

                    //Finalize position and velocity
                    pPos = peTransform.TransformPoint(lPos);
                    pVel = peTransform.TransformDirection(lVel)
                                + frameVel;
                }
                else if (!useWorldSpace && particle.remainingLifetime != particle.startLifetime)
                {
                    pPos = peTransform.TransformPoint(particle.position);
                    pVel = peTransform.TransformDirection(particle.velocity.x * xyForce,
                                                           particle.velocity.y * xyForce,
                                                           particle.velocity.z * zForce)
                                + frameVel;
                }
                else
                {
                    pPos = peTransform.TransformPoint(particle.position);
                    pVel = peTransform.TransformDirection(particle.velocity) + frameVel;
                }
                Profiler.EndSample();
                // try-finally block to ensure we set the particle velocities correctly in the end.
                try
                {
                    // Fixed update is not the best place to update the size but the particles array copy is
                    // slow so doing each frame would be worse

                    Profiler.BeginSample("Grow");
                    
                    if (sizeGrow != 0.0)
                    {
                        particle.startSize = particle.startSize * growConst;
                    }
                    // No need to waste time doing a division if the result is 0.
                    if (logarithmicGrow != 0.0)
                    {
                        // Euler integration of the derivative of Log(logarithmicGrowth * t + 1) + 1.
                        // This might look weird.
                        
                        particle.startSize += (float) ((logGrowConst / (1 + (particle.startLifetime - particle.remainingLifetime) * logarithmicGrow)) * averageSize);
                    }
                    if (linearGrow != 0.0)
                    {
                        particle.startSize += linGrowConst;
                    }
                    
                    particle.startSize = Mathf.Min(particle.startSize, sizeClamp);
                    Profiler.EndSample();
                    Profiler.BeginSample("Velocity");
                    if (particle.remainingLifetime == particle.startLifetime)
                    {
                        
                        if (useWorldSpace)
                        {
                            // Uniformly scatter newly emitted particles along the emitter's trajectory in order to
                            // remove the dotted smoke effect.
                            // use variableDeltaTime since the particle are emited on Update anyway.
                            pPos -= emitterWorldVelocity * Random.value * Time.deltaTime;
                        }

                        if (randomInitalVelocityOffsetMaxRadius != 0.0)
                        {
                            Vector2 diskPoint = Random.insideUnitCircle * randomInitalVelocityOffsetMaxRadius;
                            Vector3d offset;
                            if (pVel.x == 0.0 && pVel.y == 0.0)
                            {
                                offset = new Vector3d(diskPoint.x, diskPoint.y, 0.0);
                            }
                            else
                            {
                                // Convoluted calculations to save some operations (especially divisions).
                                // Not that it really matters, but this achieves 2 divisions and 1 square root.
                                double x = pVel.x;
                                double y = pVel.y;
                                double z = pVel.z;
                                double xSquared = x * x;
                                double ySquared = y * y;
                                double xySquareNorm = xSquared + ySquared;
                                double inverseXYSquareNorm = 1 / xySquareNorm;
                                double inverseNorm = 1 / Math.Sqrt(xySquareNorm + z * z);
                                double zOverNorm = z * inverseNorm;

                                // TODO(robin): find an identifier for that...
                                double mixedTerm = x * y * (zOverNorm - 1);
                                offset =
                                    new Vector3d(
                                        ((ySquared + xSquared * zOverNorm) * diskPoint.x + mixedTerm * diskPoint.y)
                                        * inverseXYSquareNorm,
                                        ((xSquared + ySquared * zOverNorm) * diskPoint.y + mixedTerm * diskPoint.x)
                                        * inverseXYSquareNorm,
                                        -(x * diskPoint.x + y * diskPoint.y) * inverseNorm);
                            }
                            pVel += offset;

                        }
                    }
                    Profiler.EndSample();
                    Profiler.BeginSample("ParticlePhysics");
                    if (physical && (j % physicsPass == activePhysicsPass))
                    {
                        // There must be a way to keep the actual initial volume,
                        // but I'm lazy.
                        pVel = ParticlePhysics(particle.startSize, averageSize, pPos, pVel);
                        
                    }
                    Profiler.EndSample();
                    Profiler.BeginSample("ParticleCollision");
                    if (collide && particle.remainingLifetime != particle.startLifetime

                        // Do not collide newly created particles (they collide with the emitter and things look bad).
                        && (j % physicsPass == activePhysicsPass))
                    {
                        pVel = ParticleCollision(pPos, pVel, mask);
                        
                    }
                    Profiler.EndSample();
                }
                finally
                {
                    Profiler.BeginSample("SetVelPos");
                    particle.velocity = pe.main.simulationSpace == ParticleSystemSimulationSpace.World
                        ? (Vector3)(pVel - frameVel)
                        : peTransform.InverseTransformDirection(pVel - frameVel);
                    particle.position = pe.main.simulationSpace == ParticleSystemSimulationSpace.World
                        ? (Vector3)pPos
                        : peTransform.InverseTransformPoint(pPos);
                    Profiler.EndSample();
                }

                
                if (doesAnimateColor)
                {
                    Profiler.BeginSample("AnimateColor");
                    float lifePercentage = 1 - (particle.remainingLifetime / particle.startLifetime);
                    
                    float lerp;
                    Color a;
                    Color b;
                    if (lifePercentage < 0.25f)
                    {
                        a = colors[0];
                        b = colors[1];
                        lerp = lifePercentage * 4f;
                    }
                    else if (lifePercentage < 0.50f)
                    {
                        a = colors[1];
                        b = colors[2];
                        lerp = (lifePercentage - 0.25f) * 4f;
                    }
                    else if (lifePercentage < 0.75f)
                    {
                        a = colors[2];
                        b = colors[3];
                        lerp = (lifePercentage - 0.50f) * 4f;
                    }
                    else 
                    {
                        a = colors[3];
                        b = colors[4];
                        lerp = (lifePercentage - 0.75f) * 4f;
                    }
                    
                    Color c = Color.Lerp(a, b, lerp);
                    particle.startColor = c;
                    Profiler.EndSample();
                }
            }
            Profiler.BeginSample("SetParticle");
            particles[j] = particle;
            Profiler.EndSample();
        }
        Profiler.EndSample();
        activePhysicsPass = ++activePhysicsPass % physicsPass;
        Profiler.BeginSample("SetParticles");
        pe.SetParticles(particles, numParticlesAlive);
        Profiler.EndSample();

        SmokeScreenConfig.activeParticles += pe.particleCount;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 ParticlePhysics(double radius, double initialRadius, Vector3d pPos, Vector3d pVel)
    {
        // N.B.: multiplications rather than Pow, Pow is slow,
        // multiplication by .5 rather than division by 2 (same
        // reason).
        CelestialBody mainBody = FlightGlobals.currentMainBody;
        double estimatedInitialVolume = 0.75 * Math.PI * initialRadius * initialRadius * initialRadius;
        double currentVolume = 0.75 * Math.PI * radius * radius * radius;
        double volumeChange = currentVolume - estimatedInitialVolume;
        double atmosphericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(pPos, mainBody), FlightGlobals.getExternalTemperature(pPos, FlightGlobals.currentMainBody), FlightGlobals.currentMainBody);
        double density = (estimatedInitialVolume * initialDensity + volumeChange * atmosphericDensity) / currentVolume;
        double mass = density * currentVolume;

        // Weight and buoyancy.
        Vector3d mainBodyDist = mainBody.position - pPos;
        Vector3d geeForce = mainBodyDist.normalized * (mainBody.gMagnitudeAtCenter / mainBodyDist.sqrMagnitude);
        Vector3d acceleration = (1 - (atmosphericDensity / density)) * geeForce;

        // Drag. TODO(robin): simplify.
        acceleration += -0.5 * atmosphericDensity * pVel * pVel.magnitude * dragCoefficient * Math.PI * radius * radius / mass;

        // Euler is good enough for graphics.
        return pVel + acceleration * TimeWarp.fixedDeltaTime * physicsPass;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 ParticleCollision(Vector3d pPos, Vector3d pVel, int mask)
    {
        RaycastHit hit;
        if (Physics.Raycast(
            pPos,
            pVel,
            out hit,
            (float)pVel.magnitude * TimeWarp.fixedDeltaTime * physicsPass,
            mask))
        {
            //// collidersName[hit.collider.name] = true;

            if (hit.collider.name != SmokeScreenUtil.LaunchPadGrateColliderName)
            {
                Vector3 unitTangent = (hit.normal.x == 0 && hit.normal.y == 0)
                    ? new Vector3(1, 0, 0)
                    : Vector3.ProjectOnPlane(new Vector3(0, 0, 1), hit.normal).normalized;
                 Vector3 hVel = Vector3.ProjectOnPlane(pVel, hit.normal);
                Vector3 reflectedNormalVelocity = hVel - pVel;
                float residualFlow = reflectedNormalVelocity.magnitude * (1 - collideRatio);

                // An attempt at a better velocity change; the blob collides with some
                // restitution coefficient collideRatio << 1 and we add a random tangential term
                // for outflowing particles---randomness handwaved in through fluid dynamics:
                float randomAngle = Random.value * 360.0f;
                Vector3d outflow = Quaternion.AngleAxis(randomAngle, hit.normal) * unitTangent * residualFlow;
                pVel = hVel + collideRatio * reflectedNormalVelocity + outflow * (1 - stickiness);
            }
            else
            {
                // Don't collide with the launch pad grid and add colliders under it
                if (!addedLaunchPadCollider)
                {
                    addedLaunchPadCollider = SmokeScreenUtil.AddLaunchPadColliders(hit);
                }
            }
        }
        return pVel;
    }
    private Vector3 RandomConeVector(float angle)
    {
        //Performed in the transform's frame, default output should be 0,0,1
        Vector3 unit = new Vector3(0,0,1);
        //Produce a random vector within "angle" of the original vector.
        Vector2 disk = Random.insideUnitCircle*angle;
        unit.x = disk.x;
        unit.y = disk.y;
        unit = Vector3.Normalize(unit);
        return unit;
    }
    private void Print(string s)
    {
        MonoBehaviour.print("[SmokeScreen " + GetType().Name + "] : " + s);
    }
}