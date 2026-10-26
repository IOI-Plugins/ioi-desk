chrome.action.onClicked.addListener(async tab => {
  if (!tab.id || !/^https?:/.test(tab.url || "")) return;
  try {
    const states = await chrome.scripting.executeScript({target:{tabId:tab.id},world:"MAIN",func:()=>Boolean(window.__ioiZoneFullscreen)});
    if (!states[0]?.result) {
      await chrome.scripting.executeScript({target:{tabId:tab.id,allFrames:true},world:"MAIN",files:["zone-fullscreen.js"]});
    } else {
      await chrome.scripting.executeScript({target:{tabId:tab.id,allFrames:true},world:"MAIN",func:()=>window.dispatchEvent(new CustomEvent("ioi-zone-toggle"))});
    }
  } catch (error) { console.warn("IOI Desk: não foi possível ativar nesta página",error.message); }
});
