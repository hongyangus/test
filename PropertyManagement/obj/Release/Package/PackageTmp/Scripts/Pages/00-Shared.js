$body = $("body");

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Create a spinner to indicate page loading
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
$(document).on({
    ajaxStart: function () { $body.addClass("loading"); },
    ajaxStop: function () { $body.removeClass("loading"); }
});

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Function to handle report filters and serializing them into JSON format
// NOTE: This should match the ReportFilters class in ReportParameters.cs file!!
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
function setfilters() {
    console.log("here");
    var reportfilters = {
        startDate: $("#startDate").val(),
        endDate: $("#endDate").val(),
        technologist: $("#AccountID").val(),
        modality: $("#ModalityID").val(),
        procedure: $("#ProcedureID").val(),
        radiologist: $("#RadiologistID").val(),
        site: $("#SiteID").val(),
        service: $("#ServiceID").val(),
        hourOfDay: $("#HODID").val(),
        dayOfWeek: $("#DOWID").val(),
        qGendaShift: $("#Service").val(),
        patientClass: $("#PatientClassID").val(),
        priority: $("#PriorityID").val(),
        lowerTATThreshold: $("#LowerThreshold").val(),
        upperTATThreshold: $("#UpperThreshold").val(),
        drillDownLevel: $("#DrillDownLevelID").val(),
        chartType: $("#ChartTypeID").val(),
        comparisonVariable: $("#ComparisonVariableID").val(),
        reportType: $("#Priority").val(),
        readingWithFellowType: $("#WithFellowID").val(),
        volumeCalculation: $("#VolumeCalculationTypeID").val(),
        accession: $("#accession").val()
    };
    var reportfilters_json = JSON.stringify(reportfilters);
    return reportfilters_json;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Function to display a modal window displaying the diagnostic report
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
$(document).on("click", ".ReportModalDialog", function () {
    // The base url for the web service. Called with accession number and user it returns the formatted report text.
    var webserviceurl = "http://mcraddev2/radwebservices/ps360ws.php?service=GetReportText";

    var accession = "";
    var patientname = "";
    var patientmrn = "";
    var user = "";
    var appid = "";

    if ($(this).attr('data-accession'))   { accession = $(this).data('accession'); }
    if ($(this).attr('data-patientname')) { patientname = $(this).data('patientname'); }
    if ($(this).attr('data-patientmrn'))  { patientmrn = $(this).data('patientmrn'); }
    if ($(this).attr('data-user'))        { user = $(this).data('user'); }
    if ($(this).attr('data-appid'))  { appid = $(this).data('appid'); }
    appid = encodeURIComponent(appid.trim()); // Encode any spaces in the applicationid (report title) as %20

    // Get the variables from the href in the view
    //var accession = $(this).data('accession');      // Get the value from the ReportModalDialog href in the report view - MUST BE PROVIDED!
    //var patientname = $(this).data('patientname');  // Get the value from the ReportModalDialog href in the report view if available
    //var patientmrn = $(this).data('patientmrn');    // Get the value from the ReportModalDialog href in the report view if available
    //var user = $(this).data('user');      // Get the value from the ReportModalDialog href in the report view if available

    // Build the report title for the modal dialog
    var reporttitle = "Diagnostic Report";
    if (patientname != "") { reporttitle = reporttitle + ": " + patientname; }
    if (patientmrn != "") { reporttitle = reporttitle + " (" + patientmrn + ")"; }
    //console.log("Pat Name:" + patientname);
    //console.log("Pat MRN:" + patientmrn);

    // Build the complete web service URL
    var reporturl = webserviceurl + "&accession=" + accession + "&user=" + user + "&appid=" + appid;
    console.log("ReportURL:" + reporturl);
    $('.modal-title').text(reporttitle);  // Load the report title into the modal dialog
    $('.modal-body').load(reporturl); // Load the report modal body from the web service URL
});

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Script to execute when document is ready
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
$(document).ready(function () {

    // Setup the datepicker format
    $("#startDate").datepicker();
    $("#endDate").datepicker();

    // Setup the multiple selector format
    $('.selectpicker').selectpicker({
        "actionsBox": true,
        "noneSelectedText": "Please select...",
    });

}); // End of document ready

