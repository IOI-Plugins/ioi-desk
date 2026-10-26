import AppKit

func runLayoutTests() throws {

    let input:[String:Any]=["version":1,"cells":[["id":"old","display":1],["id":"other","display":3]],"assignments":["old":"a","other":"b"]]
    let result=AppController.transformed(input,changes:[1:2]);let cells=result["cells"] as! [[String:Any]]
    precondition(cells.count==3 && cells.contains{$0["id"] as? String == "other"})
    precondition((result["assignments"] as! [String:Any])["other"] as? String == "b")
    precondition((result["assignments"] as! [String:Any])["old"] == nil)
    for cell in cells where (cell["display"] as? UInt32)==1{precondition(cell["w"] as? Double == 1)}
    print("PASS: full-width rows, target-only layout change, unaffected assignments preserved")

}
