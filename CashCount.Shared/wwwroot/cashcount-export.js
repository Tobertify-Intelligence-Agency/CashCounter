window.cashCountExports = {
  downloadFile(fileName, contentType, byteArray) {
    const bytes = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
    const blob = new Blob([bytes], { type: contentType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
};

window.cashCountSignature = (() => {
  const pads = new Map();

  const LOGICAL_WIDTH = 560;
  const LOGICAL_HEIGHT = 180;

  function getPoint(event, canvas) {
    const rect = canvas.getBoundingClientRect();
    const source = event.touches ? event.touches[0] : event;
    return {
      x: ((source.clientX - rect.left) / rect.width) * LOGICAL_WIDTH,
      y: ((source.clientY - rect.top) / rect.height) * LOGICAL_HEIGHT
    };
  }

  function drawStroke(ctx, stroke) {
    if (!stroke || !stroke.points || stroke.points.length === 0) {
      return;
    }

    ctx.beginPath();
    ctx.moveTo(stroke.points[0].x, stroke.points[0].y);
    for (let i = 1; i < stroke.points.length; i += 1) {
      ctx.lineTo(stroke.points[i].x, stroke.points[i].y);
    }
    ctx.stroke();
  }

  function redraw(state) {
    const { canvas, ctx, strokes } = state;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.lineWidth = 2.4;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.strokeStyle = '#0f172a';

    strokes.forEach(stroke => drawStroke(ctx, stroke));
  }

  function resizeCanvas(state) {
    const ratio = window.devicePixelRatio || 1;
    const width = state.canvas.clientWidth || LOGICAL_WIDTH;
    const height = state.canvas.clientHeight || LOGICAL_HEIGHT;
    state.canvas.width = Math.round(width * ratio);
    state.canvas.height = Math.round(height * ratio);
    state.ctx.setTransform((width / LOGICAL_WIDTH) * ratio, 0, 0, (height / LOGICAL_HEIGHT) * ratio, 0, 0);
    redraw(state);
  }

  function bindEvents(state) {
    const { canvas } = state;

    const start = event => {
      event.preventDefault();
      const point = getPoint(event, canvas);
      const stroke = { points: [point] };
      state.strokes.push(stroke);
      state.activeStroke = stroke;
      redraw(state);
    };

    const move = event => {
      if (!state.activeStroke) {
        return;
      }

      event.preventDefault();
      state.activeStroke.points.push(getPoint(event, canvas));
      redraw(state);
    };

    const end = event => {
      if (!state.activeStroke) {
        return;
      }

      event.preventDefault();
      if (state.activeStroke.points.length === 1) {
        state.activeStroke.points.push({ ...state.activeStroke.points[0] });
      }
      state.activeStroke = null;
      redraw(state);
    };

    canvas.addEventListener('pointerdown', start);
    canvas.addEventListener('pointermove', move);
    window.addEventListener('pointerup', end);
    canvas.addEventListener('touchstart', start, { passive: false });
    canvas.addEventListener('touchmove', move, { passive: false });
    window.addEventListener('touchend', end, { passive: false });

    state.cleanup = () => {
      canvas.removeEventListener('pointerdown', start);
      canvas.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', end);
      canvas.removeEventListener('touchstart', start);
      canvas.removeEventListener('touchmove', move);
      window.removeEventListener('touchend', end);
    };
  }

  return {
    init(canvasId, strokesJson) {
      const canvas = document.getElementById(canvasId);
      if (!canvas) {
        return false;
      }

      const existing = pads.get(canvasId);
      if (existing && existing.cleanup) {
        existing.cleanup();
      }

      const ctx = canvas.getContext('2d');
      const state = {
        canvas,
        ctx,
        strokes: [],
        activeStroke: null,
        cleanup: null
      };

      pads.set(canvasId, state);
      bindEvents(state);
      resizeCanvas(state);

      if (strokesJson) {
        try {
          state.strokes = JSON.parse(strokesJson) || [];
        } catch {
          state.strokes = [];
        }
      }

      redraw(state);
      return true;
    },
    clear(canvasId) {
      const state = pads.get(canvasId);
      if (!state) {
        return;
      }

      state.strokes = [];
      state.activeStroke = null;
      redraw(state);
    },
    getStrokes(canvasId) {
      const state = pads.get(canvasId);
      return JSON.stringify(state?.strokes || []);
    }
  };
})();
