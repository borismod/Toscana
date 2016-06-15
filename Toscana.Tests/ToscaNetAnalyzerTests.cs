﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Toscana.Domain;
using Toscana.Exceptions;

namespace Toscana.Tests
{
    [TestFixture]
    public class ToscaNetAnalyzerTests
    {
        [Test]
        public void Analyze_All_Property_Keynames_Are_Set()
        {
            const string toscaString = @"
tosca_definitions_version: tosca_simple_yaml_1_0
 
node_types:
  example.TransactionSubsystem:
    properties:
      num_cpus:
        type: integer
        description: Number of CPUs requested for a software node instance.
        default: 1
        status: experimental
        required: true
        entry_schema: default
        constraints:
          - valid_values: [ 1, 2, 4, 8 ]";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().BeNull();
            tosca.NodeTypes.Should().HaveCount(1);

            var nodeType = tosca.NodeTypes["example.TransactionSubsystem"];

            nodeType.Properties.Should().HaveCount(1);
            var numCpusProperty = nodeType.Properties["num_cpus"];
            numCpusProperty.Type.Should().Be("integer");
            numCpusProperty.Description.Should().Be("Number of CPUs requested for a software node instance.");
            numCpusProperty.Default.Should().Be("1");
            numCpusProperty.Required.Should().BeTrue();
            numCpusProperty.Status.Should().Be(PropertyStatus.experimental);
            numCpusProperty.EntrySchema.Should().Be("default");
            numCpusProperty.Constraints.Should().HaveCount(1);
            numCpusProperty.Constraints.Single().Should().HaveCount(1);
            var validValues = (List<object>) numCpusProperty.Constraints.Single()["valid_values"];
            validValues.Should().BeEquivalentTo(new List<object> {"1", "2", "4", "8"});
        }

        [Test]
        public void Analyze_Cloudshell_Base_Standard()
        {
            const string toscaString = @"# CloudShell Base Standard
# Suitable for modeling CloudShell Shells 
tosca_definitions_version: tosca_simple_yaml_1_0

node_types:

  cloudshell.standard.Shell:
    properties:
      ip:
        type: string
        default: ''
    interfaces:
      cloudshell.shell.core.resource_driver_interface:
        get_inventory: {}
        initialize: {}
        cleanup: {}
        backup: {}
        restore: {}
";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().BeNull();
            tosca.NodeTypes.Should().HaveCount(1);
            var nodeType = tosca.NodeTypes["cloudshell.standard.Shell"];
            var resourceDriverInterface = nodeType.Interfaces["cloudshell.shell.core.resource_driver_interface"];
            ((IDictionary<object, object>) resourceDriverInterface["get_inventory"]).Should().BeEmpty();
            ((IDictionary<object, object>) resourceDriverInterface["initialize"]).Should().BeEmpty();
            ((IDictionary<object, object>) resourceDriverInterface["cleanup"]).Should().BeEmpty();
            ((IDictionary<object, object>) resourceDriverInterface["backup"]).Should().BeEmpty();
            ((IDictionary<object, object>) resourceDriverInterface["restore"]).Should().BeEmpty();
        }

        [Test]
        public void Analyze_Defining_a_Subsystem_Node_Type()
        {
            const string toscaString = @"
tosca_definitions_version: tosca_simple_yaml_1_0
 
node_types:
  example.TransactionSubsystem:
    properties:
      mq_service_ip:
        type: string
      receiver_port:
        type: integer
    attributes:
      receiver_ip:
        type: string
      receiver_port:
        type: integer
    capabilities:
      message_receiver: tosca.capabilities.Endpoint
    requirements:
      - database_endpoint: tosca.capabilities.Endpoint.Database";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().BeNull();
            tosca.NodeTypes.Should().HaveCount(1);

            var nodeType = tosca.NodeTypes["example.TransactionSubsystem"];

            nodeType.Properties.Should().HaveCount(2);
            nodeType.Properties["mq_service_ip"].Type.Should().Be("string");
            nodeType.Properties["receiver_port"].Type.Should().Be("integer");

            nodeType.Attributes.Should().HaveCount(2);
            nodeType.Attributes["receiver_ip"].Type.Should().Be("string");
            nodeType.Attributes["receiver_port"].Type.Should().Be("integer");

            nodeType.Capabilities.Should().HaveCount(1);
            nodeType.Capabilities["message_receiver"].Type.Should().Be("tosca.capabilities.Endpoint");

            nodeType.Requirements.Should().HaveCount(1);
            nodeType.Requirements.Single()["database_endpoint"].Capability.Should().Be("tosca.capabilities.Endpoint.Database");
        }

        [Test]
        public void Analyze_HelloWorld_AllDataAnalyzed()
        {
            const string toscaString = @"
tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying a single server with predefined properties.
 
topology_template:
  node_templates:
    my_server:
      type: tosca.nodes.Compute
      capabilities:
        # Host container properties
        host:
         properties:
           num_cpus: 1
           disk_size: 10 GB
           mem_size: 4096 MB
        # Guest Operating System properties
        os:
          properties:
            # host Operating System image properties
            architecture: x86_64
            type: linux 
            distribution: rhel 
            version: 6.5 ";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a single server with predefined properties.");
            var nodeTemplate = tosca.TopologyTemplate.NodeTemplates["my_server"];
            nodeTemplate.Type.Should().Be("tosca.nodes.Compute");

            var host = nodeTemplate.Capabilities["host"];
            host.Properties["num_cpus"].Should().Be("1");
            host.Properties["disk_size"].Should().Be("10 GB");
            host.Properties["mem_size"].Should().Be("4096 MB");

            var os = nodeTemplate.Capabilities["os"];
            os.Properties["architecture"].Should().Be("x86_64");
            os.Properties["type"].Should().Be("linux");
            os.Properties["distribution"].Should().Be("rhel");
            os.Properties["version"].Should().Be("6.5");
        }

        [Test]
        public void Analyze_Overriding_Behavior_of_Predefined_Node_Types()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying a single server with MySQL software on top.
 
topology_template:
  inputs:
    # omitted here for brevity
 
  node_templates:
    mysql:
      type: tosca.nodes.DBMS.MySQL
      properties:
        root_password: { get_input: my_mysql_rootpw }
        port: { get_input: my_mysql_port }
      requirements:
        - host: db_server
      interfaces:
        Standard:
          configure: scripts/my_own_configure.sh
 
    db_server:
      type: tosca.nodes.Compute
      capabilities:
        # omitted here for brevity";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a single server with MySQL software on top.");
            var topologyTemplate = tosca.TopologyTemplate;

            topologyTemplate.Inputs.Should().BeNull();
            topologyTemplate.Outputs.Should().BeNull();

            topologyTemplate.NodeTemplates.Should().HaveCount(2);

            var mysqlNodeTemplate = topologyTemplate.NodeTemplates["mysql"];
            mysqlNodeTemplate.Type.Should().Be("tosca.nodes.DBMS.MySQL");
            var requirementKeyValue = mysqlNodeTemplate.Requirements.Single().Single();
            requirementKeyValue.Key.Should().Be("host");
            requirementKeyValue.Value.Node.Should().Be("db_server");
            var standardInterface = (IDictionary<object, object>) mysqlNodeTemplate.Interfaces["Standard"];
            standardInterface["configure"].Should().Be("scripts/my_own_configure.sh");

            var dbServerNodeTemplate = topologyTemplate.NodeTemplates["db_server"];
            dbServerNodeTemplate.Type.Should().Be("tosca.nodes.Compute");
            dbServerNodeTemplate.Capabilities.Should().BeNull();
            dbServerNodeTemplate.Requirements.Should().BeNull();
        }

        [Test]
        public void Analyze_Property_Keynames_Use_Defaults()
        {
            const string toscaString = @"
tosca_definitions_version: tosca_simple_yaml_1_0
 
node_types:
  example.TransactionSubsystem:
    properties:
      num_cpus:
        type: integer";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            var numCpusProperty = tosca.NodeTypes["example.TransactionSubsystem"].Properties["num_cpus"];
            numCpusProperty.Type.Should().Be("integer");
            numCpusProperty.Description.Should().BeNull();
            numCpusProperty.Default.Should().BeNull();
            numCpusProperty.Required.Should().BeTrue();
            numCpusProperty.Status.Should().Be(PropertyStatus.supported);
            numCpusProperty.EntrySchema.Should().BeNull();
            numCpusProperty.Constraints.Should().BeNull();
        }

        [Test]
        public void Analyze_Template_For_a_Simple_Software_Installation()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
description: Template for deploying a single server with MySQL software on top.
 
topology_template:
  inputs:
    # omitted here for brevity
 
  node_templates:
    mysql:
      type: tosca.nodes.DBMS.MySQL
      properties:
        root_password: { get_input: my_mysql_rootpw }
        port: { get_input: my_mysql_port }
      requirements:
        - host: db_server
 
    db_server:
      type: tosca.nodes.Compute
      capabilities:
        # omitted here for brevity";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a single server with MySQL software on top.");
            var topologyTemplate = tosca.TopologyTemplate;

            topologyTemplate.Inputs.Should().BeNull();
            topologyTemplate.Outputs.Should().BeNull();

            topologyTemplate.NodeTemplates.Should().HaveCount(2);

            var mysqlNodeTemplate = topologyTemplate.NodeTemplates["mysql"];
            mysqlNodeTemplate.Type.Should().Be("tosca.nodes.DBMS.MySQL");
            var requirementKeyValue = mysqlNodeTemplate.Requirements.Single().Single();
            requirementKeyValue.Key.Should().Be("host");
            requirementKeyValue.Value.Node.Should().Be("db_server");

            var dbServerNodeTemplate = topologyTemplate.NodeTemplates["db_server"];
            dbServerNodeTemplate.Type.Should().Be("tosca.nodes.Compute");
            dbServerNodeTemplate.Capabilities.Should().BeNull();
            dbServerNodeTemplate.Requirements.Should().BeNull();
        }

        [Test]
        public void Analyze_Template_For_A_Two_Tier_Application()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying a two-tier application servers on two
 
topology_template:
  inputs:
    # Admin user name and password to use with the WordPress application
    wp_admin_username:
      type: string
    wp_admin_password:
      type: string
    wp_db_name:
      type: string
    wp_db_user:
      type: string
    wp_db_password:
      type: string
    wp_db_port:
      type: integer
    mysql_root_password:
      type: string
    mysql_port:
      type: integer
    context_root:
      type: string
 
  node_templates:
    wordpress:
      type: tosca.nodes.WebApplication.WordPress
      properties:
        context_root: { get_input: context_root }
        admin_user: { get_input: wp_admin_username }
        admin_password: { get_input: wp_admin_password }
        db_host: { get_attribute: [ db_server, private_address ] }
      requirements:
        - host: apache
        - database_endpoint: wordpress_db
      interfaces:
        Standard:
          inputs:
            db_host: { get_attribute: [ db_server, private_address ] }
            db_port: { get_property: [ wordpress_db, port ] }
            db_name: { get_property: [ wordpress_db, name ] }
            db_user: { get_property: [ wordpress_db, user ] }
            db_password: { get_property: [ wordpress_db, password ] }  
 
    apache:
      type: tosca.nodes.WebServer.Apache
      properties:
        # omitted here for brevity
      requirements:
        - host: web_server
 
    web_server:
      type: tosca.nodes.Compute
      capabilities:
        # omitted here for brevity
 
    wordpress_db:
      type: tosca.nodes.Database.MySQL
      properties:
        name: { get_input: wp_db_name }
        user: { get_input: wp_db_user }
        password: { get_input: wp_db_password }
        port: { get_input: wp_db_port }
      requirements:
        - host: mysql
 
    mysql:
      type: tosca.nodes.DBMS.MySQL
      properties:
        root_password: { get_input: mysql_root_password }
        port: { get_input: mysql_port }
      requirements:
        - host: db_server
 
    db_server:
      type: tosca.nodes.Compute
      capabilities:
        # omitted here for brevity";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a two-tier application servers on two");
            var topologyTemplate = tosca.TopologyTemplate;

            #region Topology Template Input & Outputs

            topologyTemplate.Inputs["wp_admin_username"].Type.Should().Be("string");
            topologyTemplate.Inputs["wp_admin_password"].Type.Should().Be("string");
            topologyTemplate.Inputs["wp_db_name"].Type.Should().Be("string");
            topologyTemplate.Inputs["wp_db_password"].Type.Should().Be("string");
            topologyTemplate.Inputs["wp_db_port"].Type.Should().Be("integer");
            topologyTemplate.Inputs["mysql_root_password"].Type.Should().Be("string");
            topologyTemplate.Inputs["mysql_port"].Type.Should().Be("integer");
            topologyTemplate.Inputs["context_root"].Type.Should().Be("string");
            topologyTemplate.Outputs.Should().BeNull();

            #endregion

            topologyTemplate.NodeTemplates.Should().HaveCount(6);

            #region wordpress

            var wordpressNodeTemplate = topologyTemplate.NodeTemplates["wordpress"];
            wordpressNodeTemplate.Type.Should().Be("tosca.nodes.WebApplication.WordPress");
            wordpressNodeTemplate.Capabilities.Should().BeNull();
            wordpressNodeTemplate.Requirements.Should().HaveCount(2);
            wordpressNodeTemplate.Requirements.First()["host"].Node.Should().Be("apache");
            wordpressNodeTemplate.Requirements.Last()["database_endpoint"].Node.Should().Be("wordpress_db");
            wordpressNodeTemplate.Properties.Should().HaveCount(4);

            #endregion

            #region apache

            var apacheNodeTemplate = topologyTemplate.NodeTemplates["apache"];
            apacheNodeTemplate.Type.Should().Be("tosca.nodes.WebServer.Apache");
            apacheNodeTemplate.Capabilities.Should().BeNull();
            apacheNodeTemplate.Requirements.Should().HaveCount(1);
            apacheNodeTemplate.Requirements.Single()["host"].Node.Should().Be("web_server");
            apacheNodeTemplate.Properties.Should().BeNull();

            #endregion

            #region web_server

            var webServerNodeTemplate = topologyTemplate.NodeTemplates["web_server"];
            webServerNodeTemplate.Type.Should().Be("tosca.nodes.Compute");
            webServerNodeTemplate.Capabilities.Should().BeNull();
            webServerNodeTemplate.Requirements.Should().BeNull();
            webServerNodeTemplate.Properties.Should().BeNull();

            #endregion

            #region wordpress_db

            var wordpressDbNodeTemplate = topologyTemplate.NodeTemplates["wordpress_db"];
            wordpressDbNodeTemplate.Type.Should().Be("tosca.nodes.Database.MySQL");
            wordpressDbNodeTemplate.Capabilities.Should().BeNull();
            wordpressDbNodeTemplate.Requirements.Should().HaveCount(1);
            wordpressDbNodeTemplate.Requirements.Single()["host"].Node.Should().Be("mysql");
            wordpressDbNodeTemplate.Properties.Should().HaveCount(4);

            #endregion

            #region mysql

            var mysqlNodeTemplate = topologyTemplate.NodeTemplates["mysql"];
            mysqlNodeTemplate.Type.Should().Be("tosca.nodes.DBMS.MySQL");
            var requirementKeyValue = mysqlNodeTemplate.Requirements.Single().Single();
            requirementKeyValue.Key.Should().Be("host");
            requirementKeyValue.Value.Node.Should().Be("db_server");

            #endregion

            #region db_server

            var dbServerNodeTemplate = topologyTemplate.NodeTemplates["db_server"];
            dbServerNodeTemplate.Type.Should().Be("tosca.nodes.Compute");
            dbServerNodeTemplate.Capabilities.Should().BeNull();
            dbServerNodeTemplate.Requirements.Should().BeNull();

            #endregion
        }

        [Test]
        public void Analyze_Template_For_Database_Content_Deployment()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying MySQL and database content.
 
topology_template:
  inputs:
    # omitted here for brevity
 
  node_templates:
    my_db:
      type: tosca.nodes.Database.MySQL
      properties:
        name: { get_input: database_name }
        user: { get_input: database_user }
        password: { get_input: database_password }
        port: { get_input: database_port }
      artifacts:
        db_content:
          file: files/my_db_content.txt
          type: tosca.artifacts.File
      requirements:
        - host: mysql
      interfaces:
        Standard:
          create:
            implementation: db_create.sh
            inputs:
              # Copy DB file artifact to server’s staging area
              db_data: { get_artifact: [ SELF, db_content ] }
 
    mysql:
      type: tosca.nodes.DBMS.MySQL
      properties:
        root_password: { get_input: mysql_rootpw }
        port: { get_input: mysql_port }
      requirements:
        - host: db_server
 
    db_server:
      type: tosca.nodes.Compute
      capabilities:
        # omitted here for brevity";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying MySQL and database content.");
            var topologyTemplate = tosca.TopologyTemplate;

            topologyTemplate.Inputs.Should().BeNull();
            topologyTemplate.Outputs.Should().BeNull();

            topologyTemplate.NodeTemplates.Should().HaveCount(3);

            var mysqlNodeTemplate = topologyTemplate.NodeTemplates["mysql"];
            mysqlNodeTemplate.Type.Should().Be("tosca.nodes.DBMS.MySQL");
            var requirementKeyValue = mysqlNodeTemplate.Requirements.Single().Single();
            requirementKeyValue.Key.Should().Be("host");
            requirementKeyValue.Value.Node.Should().Be("db_server");

            var dbServerNodeTemplate = topologyTemplate.NodeTemplates["db_server"];
            dbServerNodeTemplate.Type.Should().Be("tosca.nodes.Compute");
            dbServerNodeTemplate.Capabilities.Should().BeNull();
            dbServerNodeTemplate.Requirements.Should().BeNull();

            var myDbNodeTemplate = topologyTemplate.NodeTemplates["my_db"];
            myDbNodeTemplate.Type.Should().Be("tosca.nodes.Database.MySQL");
            myDbNodeTemplate.Capabilities.Should().BeNull();
            myDbNodeTemplate.Requirements.Single()["host"].Node.Should().Be("mysql");
            myDbNodeTemplate.Properties.Should().HaveCount(4);

            myDbNodeTemplate.Artifacts.Should().HaveCount(1);
            var artifact = myDbNodeTemplate.Artifacts["db_content"];
            artifact.File.Should().Be("files/my_db_content.txt");
            artifact.Type.Should().Be("tosca.artifacts.File");
        }

        [Test]
        public void Analyze_Validation_Fails_When_Property_Type_Missing()
        {
            const string toscaString = @"
tosca_definitions_version: tosca_simple_yaml_1_0
 
node_types:
  example.TransactionSubsystem:
    properties:
      num_cpus:
        description: Property without type";

            Action action = () => new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            action.ShouldThrow<ToscaValidationException>().WithMessage("type is required on property");
        }

        [Test]
        public void Analyze_With_Inputs_AllDataAnalyzed()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying a single server with predefined properties.
 
topology_template:
  inputs:
    cpus:
      type: integer
      description: Number of CPUs for the server.
      constraints:
        - valid_values: [ 1, 2, 4, 8 ]
 
  node_templates:
    my_server:
      type: tosca.nodes.Compute
      capabilities:
        # Host container properties
        host:
          properties:
            # Compute properties
            num_cpus: { get_input: cpus }
            mem_size: 2048  MB
            disk_size: 10 GB
 
  outputs:
    server_ip:
      description: The private IP address of the provisioned server.
      value: { get_attribute: [ my_server, private_address ] }";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a single server with predefined properties.");
            var topologyTemplate = tosca.TopologyTemplate;

            var topologyInputCpus = topologyTemplate.Inputs["cpus"];
            topologyInputCpus.Type.Should().Be("integer");
            topologyInputCpus.Description.Should().Be("Number of CPUs for the server.");
            topologyInputCpus.Constraints.Single()["valid_values"].ShouldAllBeEquivalentTo(new[] {1, 2, 4, 8});

            var topologyOutput = topologyTemplate.Outputs["server_ip"];
            topologyOutput.Description.Should().Be("The private IP address of the provisioned server.");
            var getAttributeValue = ((IDictionary<object, object>) topologyOutput.Value)["get_attribute"];
            ((List<object>) getAttributeValue).ShouldAllBeEquivalentTo(new[] {"my_server", "private_address"});

            var nodeTemplate = topologyTemplate.NodeTemplates["my_server"];
            nodeTemplate.Type.Should().Be("tosca.nodes.Compute");

            nodeTemplate.Type.Should().Be("tosca.nodes.Compute");
            nodeTemplate.Capabilities.Should().NotContainKey("os");
            var hostProperties = nodeTemplate.Capabilities["host"].Properties;
            ((IDictionary<object, object>) hostProperties["num_cpus"])["get_input"].Should().Be("cpus");
            hostProperties["mem_size"].Should().Be("2048  MB");
            hostProperties["disk_size"].Should().Be("10 GB");
        }

        [Test]
        public void Analyze_Imports_Multiline_Grammar()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying a single server with predefined properties.
 
imports:
  - some_definition_file:
      file: path1/path2/file2.yaml
      repository: my_service_catalog
      namespace_uri: http://mycompany.com/tosca/1.0/platform
      namespace_prefix: mycompany";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a single server with predefined properties.");

            tosca.Imports.Should().HaveCount(1);
            tosca.Imports.Single().Should().HaveCount(1);
            tosca.Imports.Single().Single().Key.Should().Be("some_definition_file");
            var toscaImport = tosca.Imports.Single().Single().Value;
            toscaImport.File.Should().Be("path1/path2/file2.yaml");
            toscaImport.Repository.Should().Be("my_service_catalog");
            toscaImport.NamespaceUri.Should().Be("http://mycompany.com/tosca/1.0/platform");
            toscaImport.NamespacePrefix.Should().Be("mycompany");

        }

        [Test]
        //[Ignore("imports should be fixed")]
        public void Analyze_Imports_Single_Line_Grammar()
        {
            const string toscaString = @"tosca_definitions_version: tosca_simple_yaml_1_0
 
description: Template for deploying a single server with predefined properties.
 
imports:
  - some_definition_file: path1/path2/some_defs.yaml";

            var tosca = new ToscaNetAnalyzer().Analyze(toscaString);

            // Assert
            tosca.ToscaDefinitionsVersion.Should().Be("tosca_simple_yaml_1_0");
            tosca.Description.Should().Be("Template for deploying a single server with predefined properties.");

            tosca.Imports.Should().HaveCount(1);
            tosca.Imports.Single().Should().HaveCount(1);
            tosca.Imports.Single().Single().Key.Should().Be("some_definition_file");
            tosca.Imports.Single().Single().Value.File.Should().Be("path1/path2/some_defs.yaml");
        }
    }
}