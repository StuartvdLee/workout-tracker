import { describe, it, expect } from 'vitest';
import { reorder } from '../utils';

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
