// Original IOI Desk browser companion. No network calls, analytics or page-content logging.
(() => {
  "use strict";
  if (window.__ioiZoneFullscreen) return;
  const nativeRequest = Element.prototype.requestFullscreen;
  const nativeExit = Document.prototype.exitFullscreen;
  let enabled = true, active = null, snapshots = [], observer = null;
  let suppressedHints = new WeakSet();
  let button = null, style = null, savedFocus = null, scroll = null, activeClass = null;
  let popoverState = null;
  const debugEnabled = new URLSearchParams(location.search).has("ioi-zone-debug");
  const changes = ["fullscreenchange", "webkitfullscreenchange"];
  const fixed = {
    position:"fixed", inset:"0px", width:"100vw", height:"100vh",
    "min-width":"0px", "min-height":"0px", "max-width":"none", "max-height":"none",
    margin:"0px", padding:"0px", border:"0px", "z-index":"2147483000", "box-sizing":"border-box",
    transform:"none", "clip-path":"none", background:"black"
  };
  function saveStyles(element, values) {
    const previous = Object.keys(values).map(key=>[key,element.style.getPropertyValue(key),element.style.getPropertyPriority(key)]);
    snapshots.push([element,previous]);
    for (const [key,value] of Object.entries(values)) element.style.setProperty(key,value,"important");
  }
  function changeEvents(element) {
    for (const name of changes) (element?.isConnected ? element : document).dispatchEvent(new Event(name,{bubbles:true}));
    window.dispatchEvent(new Event("resize"));
  }
  function suppressUnavailableHint() {
    if(!active?.classList.contains("html5-video-player")) return;
    const video=active.querySelector("video"), box=video?.getBoundingClientRect();
    if(!video || video.readyState<2 || !box || box.width<1 || box.height<1) return;
    for(const hint of active.querySelectorAll(".ytp-tooltip,.ytp-popup,[role=tooltip]")) {
      const text=(hint.textContent || "").trim();
      if(!suppressedHints.has(hint) && /(?:tela (?:cheia|inteira) (?:está )?indispon[ií]vel|full\s?screen is unavailable)/i.test(text)) {
        suppressedHints.add(hint);saveStyles(hint,{display:"none"});
      }
    }
  }
  function exit() {
    if (!active) return Promise.resolve();
    const element = active; active = null;
    suppressedHints=new WeakSet();
    if(popoverState) {
      try {element.hidePopover();} catch (_) {}
      if(popoverState.had) element.setAttribute("popover",popoverState.value);
      else element.removeAttribute("popover");
      popoverState=null;
    }
    if(activeClass) {element.classList.remove(activeClass);activeClass=null;}
    if(element instanceof HTMLIFrameElement) element.contentWindow?.postMessage({type:"ioi-zone-exit-child"},"*");
    observer?.disconnect(); observer = null;
    button?.remove(); button = null; style?.remove(); style = null;
    for (const [node,properties] of snapshots.reverse()) {
      for (const [key,value,priority] of properties) {
        if (value) node.style.setProperty(key,value,priority); else node.style.removeProperty(key);
      }
    }
    snapshots = [];
    if (scroll) window.scrollTo(scroll.x,scroll.y);
    if (savedFocus?.isConnected) savedFocus.focus({preventScroll:true});
    changeEvents(element);
    if (window !== window.top) window.parent.postMessage({type:"ioi-zone-fullscreen",active:false},"*");
    return Promise.resolve();
  }
  function enter(element) {
    if (!element?.isConnected) return Promise.reject(new DOMException("Elemento removido","InvalidStateError"));
    if (active === element) return Promise.resolve();
    if (active) exit();
    savedFocus = document.activeElement;
    scroll = {x:window.scrollX,y:window.scrollY};
    active = element;
    // Remove clipping/containing blocks only while the selected element fills this window.
    for (let parent = element.parentElement; parent; parent = parent.parentElement) {
      saveStyles(parent,{overflow:"visible",transform:"none",filter:"none",perspective:"none",contain:"none","clip-path":"none","z-index":"auto"});
    }
    saveStyles(document.documentElement,{overflow:"hidden"});
    saveStyles(element,fixed);
    // Use the browser's own top layer, keeping the player in its original DOM
    // and viewport. Ancestor stacking contexts cannot cover the video layer.
    if (element instanceof HTMLElement && typeof element.showPopover === "function" && !element.hasAttribute("popover")) {
      popoverState={had:false,value:null};
      element.setAttribute("popover","manual");
      try { element.showPopover(); }
      catch (_) {element.removeAttribute("popover");popoverState=null;}
    }
    style = document.createElement("style");
    // Scope video rules to the selected content without altering the application's DOM tree.
    const token = "ioi-zone-"+Math.random().toString(36).slice(2);
    element.classList.add(token);
    style.textContent = `.${token} video, video.${token}{width:100%!important;height:100%!important;object-fit:contain!important}`;
    if (element.classList.contains("html5-video-player")) {
      // YouTube positions the video inside a separate, explicitly sized layer.
      // Resizing only <video> leaves that layer at the old size/offset.
      style.textContent += `.${token} .html5-video-container{position:absolute!important;inset:0!important;width:100%!important;height:100%!important;transform:none!important}
        .${token} video.html5-main-video{position:absolute!important;inset:0!important;left:0!important;top:0!important;margin:0!important;transform:none!important}`;
    }
    (document.head || document.documentElement).append(style);
    activeClass = token;
    button = document.createElement("button");
    button.textContent = "Sair da zona · Esc";
    button.setAttribute("aria-label","Sair da apresentação na zona");
    Object.assign(button.style,{position:"fixed",top:"8px",right:"8px",zIndex:"2147483647",padding:"7px 12px",border:"1px solid #667",borderRadius:"8px",color:"white",background:"#18202de8",font:"12px system-ui",cursor:"pointer"});
    button.addEventListener("click",exit);
    // A document-level z-index cannot sit above a top-layer player.
    (popoverState ? element : document.body || document.documentElement).append(button);
    observer = new MutationObserver(()=>{if(active && !active.isConnected) exit();else suppressUnavailableHint();});
    observer.observe(document.documentElement,{childList:true,subtree:true,characterData:true});
    // Keep cleanup independent of how the site changes its own fullscreen class names.
    changeEvents(element);
    requestAnimationFrame(suppressUnavailableHint);
    if(debugEnabled) setTimeout(()=>{
      if(active!==element || !button) return;
      const videos=Array.from(element.querySelectorAll("video"));
      const details=videos.map(v=>{const r=v.getBoundingClientRect(),s=getComputedStyle(v);return `${v.tagName}: ${Math.round(r.x)},${Math.round(r.y)} ${Math.round(r.width)}x${Math.round(r.height)} decoded=${v.videoWidth}x${v.videoHeight} ready=${v.readyState} paused=${v.paused} display=${s.display} visibility=${s.visibility} opacity=${s.opacity} layers=${document.elementsFromPoint(innerWidth/2,innerHeight/2).slice(0,5).map(n=>n.tagName+"#"+n.id+"."+n.className).join(",")}`;});
      button.setAttribute("aria-label",`IOI diagnóstico ${element.tagName}#${element.id}.${element.className} viewport=${innerWidth}x${innerHeight} videos=${videos.length}; ${details.join("; ")}`);
    },800);
    if (window !== window.top) window.parent.postMessage({type:"ioi-zone-fullscreen",active:true},"*");
    return Promise.resolve();
  }
  // Match the site's API while keeping the native browser window unchanged.
  Element.prototype.requestFullscreen = function(options) {
    if(!enabled) return nativeRequest.call(this,options);
    // YouTube requests fullscreen on <html>, then sizes its video through
    // nested percentage-height containers. Enlarging <html> alone leaves the
    // actual video at height zero. Present the existing player instead.
    const youtube=/(^|\.)youtube(?:-nocookie)?\.com$/.test(location.hostname);
    const player=youtube && (this===document.documentElement || this===document.body)
      ? document.querySelector("#movie_player.html5-video-player") : null;
    return enter(player || this);
  };
  if (Element.prototype.webkitRequestFullscreen) Element.prototype.webkitRequestFullscreen = Element.prototype.requestFullscreen;
  Document.prototype.exitFullscreen = function() { return active ? exit() : nativeExit.call(this); };
  if (Document.prototype.webkitExitFullscreen) Document.prototype.webkitExitFullscreen = Document.prototype.exitFullscreen;
  for (const name of ["fullscreenElement","webkitFullscreenElement"]) {
    const descriptor = Object.getOwnPropertyDescriptor(Document.prototype,name);
    if (descriptor?.configurable) Object.defineProperty(Document.prototype,name,{...descriptor,get(){return active || descriptor.get.call(this);}});
  }
  document.addEventListener("keydown",event=>{
    if(active && event.key === "Escape") {event.preventDefault();event.stopImmediatePropagation();exit();}
  },true);
  window.addEventListener("pagehide",exit);
  window.addEventListener("ioi-zone-toggle",()=>{enabled=!enabled;if(!enabled)exit();});
  window.addEventListener("message",event=>{
    if(event.source === window.parent && event.data?.type === "ioi-zone-exit-child") {exit();return;}
    if(!enabled || event.data?.type !== "ioi-zone-fullscreen") return;
    // Only accept direct child frames; no arbitrary selector or content is accepted.
    const frame = Array.from(document.querySelectorAll("iframe")).find(node=>node.contentWindow === event.source);
    if(!frame) return;
    if(event.data.active === true) enter(frame);
    else if(active === frame) exit();
  });
  Object.defineProperty(window,"__ioiZoneFullscreen",{value:{get enabled(){return enabled;},get active(){return Boolean(active);},exit},configurable:false});
})();
