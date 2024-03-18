using System;
using System.Collections.Generic;
using System.Text;
using longlib.entity;
using longlib.report;

namespace longlib.demo
{
    public class Demo
    {
        public static void ReportDisplay()
        {
			string xmlSample = @"<ROOT>
	<DETAIL>
		<LINE>
			<FIELD1>1</FIELD1>
			<FIELD2>Data line Number One</FIELD2>
		</LINE>
		<LINE>
			<FIELD1>II</FIELD1>
			<FIELD2>Data line 贰号</FIELD2>
		</LINE>
			<LINE>
			<FIELD1>Three</FIELD1>
			<FIELD2>Data line No. 3</FIELD2>
		</LINE>
			<LINE>
			<FIELD1>四</FIELD1>
			<FIELD2>Data line # IV</FIELD2>
		</LINE>
	</DETAIL>
</ROOT>";

            string xsltSampel = @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
	<xsl:template match='/'>
	<div>
		<xsl:apply-templates select='/ROOT/DETAIL' mode='DETAIL' />
	</div>
	</xsl:template>
	<xsl:template match='DETAIL' mode='DETAIL'>
	<table style='font-family:Segoe UI;font-size:13px;text-align:center;table-layout:fixed;margin:0px auto;' border='1'>
		<tr class='table_header'>
			<td style='background-color:#777;color:#fff'>Field1</td>
			<td style='background-color:#777;color:#fff'>Field2</td>
		</tr>
		<xsl:for-each select='LINE'>
		<tr>
			<xsl:attribute name='STYLE'>background-color:
			  <xsl:choose>
				<xsl:when test='FIELD1 = 1'>Red;</xsl:when>
				<xsl:when test='FIELD1 = II'>Yellow;</xsl:when>
				<xsl:otherwise>Green;</xsl:otherwise>
			  </xsl:choose>
			</xsl:attribute>
			<td><xsl:value-of select='FIELD1'/></td>
			<td><xsl:value-of select='FIELD2'/></td>
		</tr>
		</xsl:for-each>
	</table>
	</xsl:template>
</xsl:stylesheet>";

			ReportConfig reportConfig = new ReportConfig
			{
				Id = "Test1",
				Title = "Test Report",
				XsltTemplate = xsltSampel,
				DataType = ReportDataType.XML
			};

			Report report = new Report(reportConfig, xmlSample);
		}
    }
}
