import { describe, it, expect } from 'vitest';
import { reorder, shuffle, applyOrder, getEffortColour, normaliseValue, buildYTicks, buildXLabels } from '../utils';

describe('getEffortColour', () => {
  it('returns #22C55E for value 1 (Easy)', () => {
    expect(getEffortColour(1)).toBe('#22C55E');
  });
  it('returns #4ADE80 for value 2 (Easy)', () => {
    expect(getEffortColour(2)).toBe('#4ADE80');
  });
  it('returns #84CC16 for value 3 (Easy)', () => {
    expect(getEffortColour(3)).toBe('#84CC16');
  });
  it('returns #A3E635 for value 4 (Moderate)', () => {
    expect(getEffortColour(4)).toBe('#A3E635');
  });
  it('returns #EAB308 for value 5 (Moderate)', () => {
    expect(getEffortColour(5)).toBe('#EAB308');
  });
  it('returns #F59E0B for value 6 (Moderate)', () => {
    expect(getEffortColour(6)).toBe('#F59E0B');
  });
  it('returns #F97316 for value 7 (Hard)', () => {
    expect(getEffortColour(7)).toBe('#F97316');
  });
  it('returns #EA580C for value 8 (Hard)', () => {
    expect(getEffortColour(8)).toBe('#EA580C');
  });
  it('returns #EF4444 for value 9 (All Out)', () => {
    expect(getEffortColour(9)).toBe('#EF4444');
  });
  it('returns #DC2626 for value 10 (All Out)', () => {
    expect(getEffortColour(10)).toBe('#DC2626');
  });
  it('returns empty string for out-of-range value 0', () => {
    expect(getEffortColour(0)).toBe('');
  });
});

describe('reorder', () => {
  it('moves the first element to the last position', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 0, 2);
    expect(arr).toEqual(['b', 'c', 'a']);
  });

  it('moves the last element to the first position', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 2, 0);
    expect(arr).toEqual(['c', 'a', 'b']);
  });

  it('moves a middle element to the front', () => {
    const arr = ['a', 'b', 'c', 'd'];
    reorder(arr, 2, 0);
    expect(arr).toEqual(['c', 'a', 'b', 'd']);
  });

  it('does nothing when fromIndex equals toIndex', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 1, 1);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('does nothing for a single-element array', () => {
    const arr = ['a'];
    reorder(arr, 0, 0);
    expect(arr).toEqual(['a']);
  });

  it('does nothing when fromIndex is out of bounds', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 5, 0);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('does nothing when toIndex is out of bounds', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, 0, 5);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('does nothing when fromIndex is negative', () => {
    const arr = ['a', 'b', 'c'];
    reorder(arr, -1, 0);
    expect(arr).toEqual(['a', 'b', 'c']);
  });

  it('works with numeric arrays', () => {
    const arr = [1, 2, 3, 4];
    reorder(arr, 3, 1);
    expect(arr).toEqual([1, 4, 2, 3]);
  });
});

describe('shuffle', () => {
  it('returns an empty array for an empty input', () => {
    expect(shuffle([])).toEqual([]);
  });

  it('returns a new array containing the single element for a length-1 input', () => {
    const result = shuffle(['a']);
    expect(result).toEqual(['a']);
  });

  it('returns a new array containing both original elements for a length-2 input', () => {
    const result = shuffle(['a', 'b']);
    expect(result).toHaveLength(2);
    expect(result).toContain('a');
    expect(result).toContain('b');
  });

  it('returns all original elements with no duplicates and no missing items', () => {
    const input = ['a', 'b', 'c', 'd', 'e'];
    const result = shuffle(input);
    expect(result).toHaveLength(input.length);
    expect([...result].sort()).toEqual([...input].sort());
  });

  it('does not mutate the original array', () => {
    const input = ['a', 'b', 'c'];
    const inputCopy = [...input];
    shuffle(input);
    expect(input).toEqual(inputCopy);
  });

  it('returns a new array instance', () => {
    const input = ['a', 'b', 'c'];
    const result = shuffle(input);
    expect(result).not.toBe(input);
  });

  it('preserves generic type (numbers)', () => {
    const nums = [1, 2, 3, 4];
    const result = shuffle(nums);
    expect(result).toHaveLength(4);
    result.forEach(n => expect(typeof n).toBe('number'));
  });
});

describe('applyOrder', () => {
  const exercises = [
    { exerciseId: 'a', name: 'A' },
    { exerciseId: 'b', name: 'B' },
    { exerciseId: 'c', name: 'C' },
  ];

  it('returns exercises in the specified order', () => {
    const result = applyOrder(exercises, ['c', 'a', 'b']);
    expect(result.map(e => e.exerciseId)).toEqual(['c', 'a', 'b']);
  });

  it('ignores IDs not present in the exercises array', () => {
    const result = applyOrder(exercises, ['c', 'unknown-id', 'a', 'b']);
    expect(result.map(e => e.exerciseId)).toEqual(['c', 'a', 'b']);
  });

  it('appends exercises not present in the order list to the end', () => {
    const result = applyOrder(exercises, ['b']);
    expect(result.map(e => e.exerciseId)).toEqual(['b', 'a', 'c']);
  });

  it('returns a copy of the exercises array when order is empty', () => {
    const result = applyOrder(exercises, []);
    expect(result.map(e => e.exerciseId)).toEqual(['a', 'b', 'c']);
  });

  it('returns a copy of the exercises array when no order IDs are valid', () => {
    const result = applyOrder(exercises, ['x', 'y', 'z']);
    expect(result.map(e => e.exerciseId)).toEqual(['a', 'b', 'c']);
  });

  it('does not mutate the original exercises array', () => {
    const input = [{ exerciseId: 'a', name: 'A' }, { exerciseId: 'b', name: 'B' }];
    applyOrder(input, ['b', 'a']);
    expect(input[0].exerciseId).toBe('a');
    expect(input[1].exerciseId).toBe('b');
  });

  it('preserves the full exercise object (not just the ID)', () => {
    const result = applyOrder(exercises, ['b', 'a', 'c']);
    expect(result[0]).toEqual({ exerciseId: 'b', name: 'B' });
  });
});

describe('normaliseValue', () => {
  it('returns 120 when min === max (flat line case)', () => {
    expect(normaliseValue(5, 5, 5)).toBe(120);
  });

  it('returns 220 (bottom) when value === min', () => {
    expect(normaliseValue(0, 0, 10)).toBe(220);
  });

  it('returns 20 (top) when value === max', () => {
    expect(normaliseValue(10, 0, 10)).toBe(20);
  });

  it('returns correct midpoint for value halfway between min and max', () => {
    expect(normaliseValue(5, 0, 10)).toBe(120);
  });

  it('handles negative min correctly', () => {
    // value=-5, min=-10, max=0 → fraction=0.5 → 220 - 0.5*200 = 120
    expect(normaliseValue(-5, -10, 0)).toBe(120);
  });

  it('returns 220 at min when min is negative', () => {
    expect(normaliseValue(-10, -10, 0)).toBe(220);
  });

  it('returns 20 at max when min is negative', () => {
    expect(normaliseValue(0, -10, 0)).toBe(20);
  });
});

describe('buildYTicks', () => {
  it('returns [min, max] for tickCount 2', () => {
    expect(buildYTicks(0, 10, 2)).toEqual([0, 10]);
  });

  it('returns 6 evenly spaced ticks for effort axis: [0,2,4,6,8,10]', () => {
    expect(buildYTicks(0, 10, 6)).toEqual([0, 2, 4, 6, 8, 10]);
  });

  it('returns [min] for tickCount < 2', () => {
    expect(buildYTicks(0, 10, 1)).toEqual([0]);
    expect(buildYTicks(0, 10, 0)).toEqual([0]);
  });

  it('returns 3 evenly spaced ticks', () => {
    expect(buildYTicks(0, 100, 3)).toEqual([0, 50, 100]);
  });
});

describe('buildXLabels', () => {
  it('returns empty array for empty input', () => {
    expect(buildXLabels([], 5)).toEqual([]);
  });

  it('returns all non-null labels when dates.length <= maxLabels', () => {
    const dates = ['2026-04-01T00:00:00Z', '2026-04-08T00:00:00Z'];
    const result = buildXLabels(dates, 5);
    expect(result).toHaveLength(2);
    expect(result.every(l => l !== null)).toBe(true);
  });

  it('always includes the last date as non-null', () => {
    const dates = ['2026-01-01T00:00:00Z', '2026-02-01T00:00:00Z', '2026-03-01T00:00:00Z',
                   '2026-04-01T00:00:00Z', '2026-05-01T00:00:00Z'];
    const result = buildXLabels(dates, 2);
    expect(result[4]).not.toBeNull();
  });

  it('returns null for intermediate dates when dates.length > maxLabels', () => {
    const dates = ['2026-01-01T00:00:00Z', '2026-02-01T00:00:00Z', '2026-03-01T00:00:00Z',
                   '2026-04-01T00:00:00Z', '2026-05-01T00:00:00Z'];
    const result = buildXLabels(dates, 2);
    expect(result).toHaveLength(5);
    const nonNullCount = result.filter(l => l !== null).length;
    expect(nonNullCount).toBeLessThanOrEqual(2);
  });

  it('formats dates as DD MMM', () => {
    const dates = ['2026-04-01T00:00:00Z'];
    const result = buildXLabels(dates, 5);
    // Should be like "01 Apr"
    expect(result[0]).toMatch(/\d{2}\s[A-Za-z]{3}/);
  });

  it('returns array of same length as input with single date', () => {
    const dates = ['2026-04-01T00:00:00Z'];
    const result = buildXLabels(dates, 5);
    expect(result).toHaveLength(1);
    expect(result[0]).not.toBeNull();
  });
});
