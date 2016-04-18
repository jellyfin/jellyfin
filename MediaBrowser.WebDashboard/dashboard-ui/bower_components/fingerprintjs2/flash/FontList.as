package {
  import flash.display.Sprite;
  import flash.display.LoaderInfo;
  import flash.text.Font;
  import flash.external.ExternalInterface;
  
  public class FontList extends Sprite {
    
    public function FontList() {
      var params:Object = loadParams();
      loadExternalInterface(params);
    }
    
    private function loadParams():Object {
      return LoaderInfo(this.root.loaderInfo).parameters;
    }
    
    private function loadExternalInterface(params:Object):void {
      ExternalInterface.call(params.onReady, fonts());
    }
    
    private function fonts():Array {
      var fontNames:Array = [];
      for each (var font:Font in Font.enumerateFonts(true) )
      {
        fontNames.push(font.fontName);
      }
      return fontNames;
    }
  }
}
