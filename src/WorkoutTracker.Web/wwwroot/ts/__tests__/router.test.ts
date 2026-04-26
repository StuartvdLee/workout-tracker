import { describe, it, expect } from 'vitest';
import { normalisePath } from '../router';

describe('normalisePath', () => {
  it('returns / for empty string', () => {
    expect(normalisePath('')).toBe('/');
  });

  it('returns / for /', () => {
    expect(normalisePath('/')).toBe('/');
  });

  it('returns path unchanged when no trailing slash', () => {
    expect(normalisePath('/workouts')).toBe('/workouts');
  });

  it('removes a single trailing slash', () => {
    expect(normalisePath('/workouts/')).toBe('/workouts');
  });

  it('removes multiple trailing slashes', () => {
    expect(normalisePath('/workouts///')).toBe('/workouts');
  });

  it('handles nested paths', () => {
    expect(normalisePath('/workouts/123')).toBe('/workouts/123');
  });

  it('removes trailing slashes from nested paths', () => {
    expect(normalisePath('/workouts/123/')).toBe('/workouts/123');
  });
});
