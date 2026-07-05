import { useEffect, useState } from 'react';

// The current viewport width, tracked across resizes. Used to switch the workspace between the wide
// (three-column) and mobile (single-column + drawer) layouts.
export function useViewport() {
  const [width, setWidth] = useState(() => (typeof window === 'undefined' ? 1280 : window.innerWidth));
  useEffect(() => {
    const onResize = () => setWidth(window.innerWidth);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);
  return width;
}
