// Script to execute when document is ready
$(document).ready(function () {

    // Get the table caption (if there is one) for the report table
    var tablecaption = $("#datatable").find('caption').text();

    // Setup standard Datatable formatting
    $('#datatable').DataTable({
        "order": [],
        "searching": true,
        "paging": true,
        "scrollCollapse": true,
        "processing": false,
        "serverSide": false,
        "iDisplayLength": 25,
        dom: 'if<"savebuttons"B>tlp',
        buttons: [
                {
                    extend: 'copyHtml5',
                    header: true,
                    footer: true,
                    message: tablecaption
                },
                {
                    extend: 'csvHtml5',
                    header: true,
                    footer: true,
                    message: tablecaption
                },
                {
                    extend: 'pdfHtml5',
                    header: true,
                    footer: true,
                    orientation: 'landscape',
                    message: tablecaption
                },
                {
                    extend: 'excelHtml5',
                    header: true,
                    footer: true,
                    title: tablecaption,
                    message: tablecaption,
                    sTitle: tablecaption
                },
                {
                    extend: 'print',
                    header: true,
                    footer: true,
                    message: tablecaption
                }
        ],
        "language": {
            loadingRecords: "Loading data, please wait...",
            zeroRecords: "No data..."
        },
        "formatNumber": function (toFormat) { return toFormat.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");  },
    }); // End of document datatable

}); // End of document ready

