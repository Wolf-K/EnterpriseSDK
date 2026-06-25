function openTab(evt, tabName) {
  var i, tabcontent, tablinks;
  tabcontent = document.getElementsByClassName("tabcontent");
  for (i = 0; i < tabcontent.length; i++) {
    tabcontent[i].style.display = "none";
    if (tabcontent[i].id.endsWith(tabName)) {
      tabcontent[i].style.display = "block";
    }
  }
  tablinks = document.getElementsByClassName("tablinks");
  for (i = 0; i < tablinks.length; i++) {
    tablinks[i].className = tablinks[i].className.replace(" active", "");
    if (tablinks[i].id.endsWith(tabName)) {
      tablinks[i].className += " active";
    }
  }
}