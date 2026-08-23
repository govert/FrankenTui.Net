// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/animation/spring.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: upstream feature-gated tracing spans are not emitted by this core type.

using System.Globalization;

namespace FrankenTui.Core;

/// <summary>A damped harmonic oscillator producing physically based motion.</summary>
public sealed class Spring : IAnimation
{
    private const double MaxStepSeconds = 0.004;
    private const double DefaultRestThreshold = 0.001;
    private const double DefaultVelocityThreshold = 0.01;
    private const double MinimumStiffness = 0.1;

    private double _position;
    private double _velocity;
    private double _target;
    private readonly double _initial;
    private double _stiffness;
    private double _damping;
    private double _restThreshold;
    private double _velocityThreshold;
    private bool _atRest;

    public Spring(double initial, double target)
    {
        _position = initial;
        _target = target;
        _initial = initial;
        _stiffness = 170.0;
        _damping = 26.0;
        _restThreshold = DefaultRestThreshold;
        _velocityThreshold = DefaultVelocityThreshold;
    }

    private Spring(Spring source)
    {
        _position = source._position;
        _velocity = source._velocity;
        _target = source._target;
        _initial = source._initial;
        _stiffness = source._stiffness;
        _damping = source._damping;
        _restThreshold = source._restThreshold;
        _velocityThreshold = source._velocityThreshold;
        _atRest = source._atRest;
    }

    public static Spring Normalized() => new(0.0, 1.0);

    public double Position => _position;

    public double Velocity => _velocity;

    public double Target => _target;

    public double Stiffness => _stiffness;

    public double Damping => _damping;

    // Mirrors Rust unit-test visibility from the containing module without
    // widening the public API surface.
    internal double RestThreshold => _restThreshold;

    internal double VelocityThreshold => _velocityThreshold;

    public bool IsAtRest => _atRest;

    public bool IsComplete => _atRest;

    public float Value => Math.Clamp((float)_position, 0.0f, 1.0f);

    public TimeSpan Overshoot => TimeSpan.Zero;

    public Spring WithStiffness(double stiffness)
    {
        _stiffness = RustMax(stiffness, MinimumStiffness);
        return this;
    }

    public Spring WithDamping(double damping)
    {
        _damping = RustMax(damping, 0.0);
        return this;
    }

    public Spring WithRestThreshold(double threshold)
    {
        _restThreshold = Math.Abs(threshold);
        return this;
    }

    public Spring WithVelocityThreshold(double threshold)
    {
        _velocityThreshold = Math.Abs(threshold);
        return this;
    }

    public void SetTarget(double target)
    {
        if (Math.Abs(_target - target) > _restThreshold)
        {
            _target = target;
            _atRest = false;
        }
    }

    public void Impulse(double velocityDelta)
    {
        _velocity += velocityDelta;
        _atRest = false;
    }

    public double CriticalDamping() => 2.0 * Math.Sqrt(_stiffness);

    public void Advance(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        if (_atRest)
        {
            return;
        }

        var remaining = delta.TotalSeconds;
        if (remaining <= 0.0)
        {
            return;
        }

        while (remaining > 0.0)
        {
            var step = Math.Min(remaining, MaxStepSeconds);
            Integrate(step);
            remaining -= step;
        }

        var positionDelta = Math.Abs(_position - _target);
        var velocityMagnitude = Math.Abs(_velocity);
        if (positionDelta < _restThreshold && velocityMagnitude < _velocityThreshold)
        {
            _position = _target;
            _velocity = 0.0;
            _atRest = true;
        }
    }

    public void Tick(TimeSpan delta) => Advance(delta);

    public void Reset()
    {
        _position = _initial;
        _velocity = 0.0;
        _atRest = false;
    }

    public Spring Clone() => new(this);

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"Spring {{ position: {_position}, velocity: {_velocity}, target: {_target}, stiffness: {_stiffness}, damping: {_damping}, at_rest: {_atRest.ToString().ToLowerInvariant()} }}");

    private void Integrate(double deltaSeconds)
    {
        var displacement = _position - _target;
        var springForce = -_stiffness * displacement;
        var dampingForce = -_damping * _velocity;
        var acceleration = springForce + dampingForce;

        _velocity += acceleration * deltaSeconds;
        _position += _velocity * deltaSeconds;
    }

    private static double RustMax(double value, double minimum) =>
        double.IsNaN(value) ? minimum : Math.Max(value, minimum);
}

/// <summary>Common spring configurations for UI motion.</summary>
public static class SpringPresets
{
    public static Spring Gentle() => Spring.Normalized().WithStiffness(120.0).WithDamping(20.0);

    public static Spring Bouncy() => Spring.Normalized().WithStiffness(300.0).WithDamping(10.0);

    public static Spring Stiff() => Spring.Normalized().WithStiffness(400.0).WithDamping(38.0);

    public static Spring Critical()
    {
        const double stiffness = 170.0;
        return Spring.Normalized()
            .WithStiffness(stiffness)
            .WithDamping(2.0 * Math.Sqrt(stiffness));
    }

    public static Spring Slow() => Spring.Normalized().WithStiffness(50.0).WithDamping(14.0);
}
