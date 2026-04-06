# Geodetic to Cartesian Conversion — Derivation

Reference for `Ellipsoid.ToVector3D()`.

---

## The Ellipsoid

An oblate ellipsoid of revolution with equatorial radius **a** and polar radius **b**:

```
  x² + y²     z²
  ——————— + ———— = 1
    a²        b²
```

First eccentricity squared: **e² = 1 − b²/a²**

## Geodetic Surface Normal

The outward unit normal to the ellipsoid is the normalized gradient of the implicit surface:

```
  n̂ = normalize( x/a², y/a², z/b² )
```

Geodetic latitude **φ** is defined as the angle this normal makes with the equatorial plane. When expressed in terms of **φ** and longitude **λ**:

```
  n̂ = ( cos φ cos λ,  cos φ sin λ,  sin φ )
```

## Parametric Equations

Solving the ellipsoid equation and normal-angle equation simultaneously for a surface point at geodetic latitude **φ** and longitude **λ** gives:

```
  x = N cos φ cos λ
  y = N cos φ sin λ
  z = N (1 − e²) sin φ
```

where:

```
              a²                        a
  N(φ) = ————————————————————— = ———————————————————
          √(a² cos²φ + b² sin²φ)   √(1 − e² sin²φ)
```

**N** is the **prime vertical radius of curvature** — the distance from the surface point to where its normal intersects the z-axis. It is *not* the distance from the origin to the surface. At the equator N = a; at the poles N = a²/b.

## The Efficient Computation

Define:

| Variable | Expression | Meaning |
|----------|------------|---------|
| **n** | (cos φ cos λ, cos φ sin λ, sin φ) | Unit geodetic surface normal |
| **k** | (a², a², b²) ⊙ **n** | Unnormalized surface position (overshoots) |
| **γ** | √(**k** · **n**) = √(a² cos²φ + b² sin²φ) | Denominator of N, i.e. N = a²/γ |
| **k**/γ | (N cos φ cos λ, N cos φ sin λ, N(1−e²) sin φ) | Point on the ellipsoid surface |

Adding height:

```
  r = k/γ + h·n̂
```

This avoids computing **N** explicitly — one component-wise multiply, one dot product, one division.
