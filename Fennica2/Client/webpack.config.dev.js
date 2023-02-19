const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = {

    mode: 'development',

    watch: true,

    entry: path.resolve(__dirname, './main.ts'),

    output: {
        path: path.resolve(__dirname, '../Build/js'),
        filename: 'bundle.js',
    },

    devtool: 'inline-source-map',

    plugins: [
        new MiniCssExtractPlugin({
            filename: '../css/style.css',
        })
    ],
    
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
            {
                test: /\.css$/i,
                use: [MiniCssExtractPlugin.loader, 'css-loader'],
            },
            {
                test: /\.(png|svg|jpg|gif|woff)$/,
                use: 'file-loader',
            }
        ]
    },

    resolve: {
        extensions: [ '.tsx', '.ts', '.js' ],
    }
    
};
